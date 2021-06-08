using scalus.Platform;
using System.Collections.Generic;
using System.Linq;
using scalus.Dto;

namespace scalus
{
    class Registration : IRegistration
    {
        private IEnumerable<IProtocolRegistrar> Registrars { get; }
        private IUserInteraction UserInteraction { get; }
        private IOsServices OsServices { get; }

        public Registration(IEnumerable<IProtocolRegistrar> registrars, IUserInteraction userInteraction, 
            IOsServices osServices
        )
        {
            Registrars = registrars;
            UserInteraction = userInteraction;
            OsServices = osServices;
        }

        public bool IsRegistered(string protocol, bool useSudo = false)
        {
            var registered = true;
            foreach (var registrar in Registrars)
            {
                if (useSudo)
                    registrar.UseSudo = true;

                registered = registered && registrar.IsScalusRegistered(protocol);
            }
            return registered;
        }

        
        public bool Register(IEnumerable<string> protocols, bool force, bool rootMode = false, bool useSudo=false)
        {
            var retval = false;

            foreach (var protocol in protocols)
            {
                retval = true;

                foreach (var registrar in Registrars)
                {
                    if (rootMode)
                        registrar.RootMode = true;
                    if (useSudo)
                        registrar.UseSudo = true;
                    if (registrar.IsScalusRegistered(protocol))
                    { 
                        UserInteraction.Message($"{protocol}: {registrar.Name}: nothing to do (scalus is already registered)...");
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
                        res = registrar.ReplaceRegistration(protocol);
                    }
                    else
                    {
                        res = registrar.Register(protocol);
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
                UserInteraction.Message($"{protocol}: Finished registering SCALUS for protocol {protocol}.");
            }
            return retval;
        }

        public bool UnRegister(IEnumerable<string> protocols, bool rootMode = false, bool useSudo= false)
        {
            foreach (var protocol in protocols)
            {
                foreach (var registrar in Registrars)
                {
                    if (rootMode)
                        registrar.RootMode = true;
                    if (useSudo)
                        registrar.UseSudo = true;

                    if (registrar.IsScalusRegistered(protocol))
                    {
                        if (!registrar.Unregister(protocol))
                        {
                            UserInteraction.Error($"{protocol}: Unable to remove scalus from {registrar.Name}. Try running this program again with administrator privileges.");
                            return false;
                        }
                    }
                    else
                    {
                        UserInteraction.Message($"{protocol}: {registrar.Name}: nothing to do (scalus is not registered) ...");
                    }
                }
                UserInteraction.Message($"{protocol}: Finished unregistering SCALUS for protocol {protocol}.");
            }
            return true;
        }
    }
}
