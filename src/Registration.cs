using System.Collections.Generic;

namespace scalus
{
    class Registration : IRegistration
    {
        private IEnumerable<IProtocolRegistrar> Registrars { get; }
        private IUserInteraction UserInteraction { get; }
        public Registration(IEnumerable<IProtocolRegistrar> registrars, IUserInteraction userInteraction)
        {
            Registrars = registrars;
            UserInteraction = userInteraction;
        }

        public bool Register(IEnumerable<string> protocols, bool force)
        {
            var retval = false;
            foreach (var protocol in protocols)
            {
                retval = false;
                foreach (var registrar in Registrars)
                {
                    var command = registrar.GetRegisteredCommand(protocol);
                    if (!string.IsNullOrEmpty(command) && !registrar.IsScalusRegistered(protocol) && !force)
                    {
                        UserInteraction.Error($"{protocol}: Protocol is already registered by another application ({command}). Use -f to overwrite.");
                        continue;
                    }

                    if (!registrar.Unregister(protocol))
                    {
                        UserInteraction.Error($"{protocol}: Unable to remove existing protocol registration. Try running this program again with administrator privileges.");
                        continue;
                    }

                    if (registrar.Register(protocol))
                    {
                        UserInteraction.Message($"{protocol}: Successfully registered SCALUS as the default protocol handler.");
                    }
                    else
                    {
                        UserInteraction.Error($"{protocol}: Failed to register SCALUS as the default protocol handler. Try running this program again with administrator privileges.");
                        continue;
                    }
                    retval = true;
                }
                if (retval == false)
                {
                    UserInteraction.Error($"Failed to register {protocol}");
                    return false;
                }
            }
            return retval;
        }

        public bool UnRegister(IEnumerable<string> protocols)
        {
            foreach (var protocol in protocols)
            {
                foreach (var registrar in Registrars)
                {
                    if (registrar.IsScalusRegistered(protocol))
                    {
                        if (!registrar.Unregister(protocol))
                        {
                            UserInteraction.Error($"{protocol}: Unable to remove SCALUS protocol registration. Try running this program again with administrator privileges.");
                            return false;
                        }
                    }
                    else
                    {
                        UserInteraction.Error($"{protocol}: SCALUS does not appear to be registered for this protocol, skipping.");
                    }
                }
                UserInteraction.Message($"{protocol}: Unregistered SCALUS as default URL protocol handler.");
            }
            return true;
        }
    }
}
