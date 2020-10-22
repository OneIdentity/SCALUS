using System.Collections.Generic;

namespace Sulu
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
                    if (!string.IsNullOrEmpty(command) && !registrar.IsSuluRegistered(protocol) && !force)
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
                        UserInteraction.Message($"{protocol}: Successfully registered Sulu as the default protocol handler.");
                    }
                    else
                    {
                        UserInteraction.Error($"{protocol}: Failed to register Sulu as the default protocol handler. Try running this program again with administrator privileges.");
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
                    if (registrar.IsSuluRegistered(protocol))
                    {
                        if (!registrar.Unregister(protocol))
                        {
                            UserInteraction.Error($"{protocol}: Unable to remove Sulu protocol registration. Try running this program again with administrator privileges.");
                            return false;
                        }
                    }
                    else
                    {
                        UserInteraction.Error($"{protocol}: Sulu does not appear to be registered for this protocol, skipping.");
                    }
                }
                UserInteraction.Message($"{protocol}: Unregistered Sulu as default URL protocol handler.");
            }
            return true;
        }
    }
}
