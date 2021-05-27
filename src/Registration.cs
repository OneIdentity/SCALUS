using scalus.Platform;
using System.Collections.Generic;

namespace scalus
{
    class Registration : IRegistration
    {
        private IEnumerable<IProtocolRegistrar> Registrars { get; }
        private IUserInteraction UserInteraction { get; }
        private IOsServices OsServices { get; }
        public Registration(IEnumerable<IProtocolRegistrar> registrars, IUserInteraction userInteraction, 
            IOsServices osServices)
        {
            Registrars = registrars;
            UserInteraction = userInteraction;
            OsServices = osServices;
        }

        public bool IsRegistered(string protocol)
        {
            var registered = true;
            foreach (var registrar in Registrars)
            {
                registered = registered && registrar.IsScalusRegistered(protocol);
            }
            return registered;
        }
        public bool Register(IEnumerable<string> protocols, bool force, bool userMode = false, bool useSudo=false)
        {
            var retval = false;

            foreach (var protocol in protocols)
            {
                retval = true;

                foreach (var registrar in Registrars)
                {
                    if (registrar.IsScalusRegistered(protocol))
                    { 
                        UserInteraction.Message($"{protocol}: {registrar.Name}: nothing to do...");
                        continue;
                    }
                    var command = registrar.GetRegisteredCommand(protocol);
                    var res = false;
                    if (!string.IsNullOrEmpty(command))
                    {
                        if (!force)
                        {
                            UserInteraction.Error(
                                $"{protocol}: another application is already registered with {registrar.Name} to launch:{command}. Use -f to overwrite.");
                            continue;
                        }
                        res = registrar.ReplaceRegistration(protocol, userMode, useSudo);
                    }
                    else
                    {
                        res = registrar.Register(protocol, userMode, useSudo);
                    }
                    if (!res)
                    {
                        UserInteraction.Error($"{protocol}: Failed to register SCALUS with {registrar.Name} as the default protocol handler. Try running this program again with administrator privileges.");
                        retval = false;
                    }
                }
                if (retval == false)
                {
                    UserInteraction.Error($"Failed to register {protocol}");
                    continue;
                }
                UserInteraction.Message($"{protocol}: Registered SCALUS as default URL protocol handler.");
            }
            return retval;
        }

        public bool UnRegister(IEnumerable<string> protocols, bool userMode = false, bool useSudo= false)
        {
            foreach (var protocol in protocols)
            {
                foreach (var registrar in Registrars)
                {
                    if (registrar.IsScalusRegistered(protocol))
                    {
                        if (!registrar.Unregister(protocol, userMode, useSudo))
                        {
                            UserInteraction.Error($"{protocol}: Unable to remove scalus from {registrar.Name}. Try running this program again with administrator privileges.");
                            return false;
                        }
                    }
                    else
                    {
                        UserInteraction.Message($"{protocol}: {registrar.Name}: nothing to do...");
                    }
                }
                UserInteraction.Message($"{protocol}: Unregistered SCALUS as default URL protocol handler.");
            }
            return true;
        }
    }
}
