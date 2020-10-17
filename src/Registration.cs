using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu
{
    class Registration : IRegistration
    {
        private IProtocolRegistrar Registrar { get; }
        private IUserInteraction UserInteraction { get; }
        public Registration(IProtocolRegistrar registrar, IUserInteraction userInteraction)
        {
            Registrar = registrar;
            UserInteraction = userInteraction;
        }

        public bool Register(IEnumerable<string> protocols, bool force)
        {
            foreach (var protocol in protocols)
            {
                var command = Registrar.GetRegisteredCommand(protocol);
                if (!string.IsNullOrEmpty(command) && !Registrar.IsSuluRegistered(protocol) && !force)
                {
                    UserInteraction.Error($"{protocol}: Protocol is already registered by another application ({command}). Use -f to overwrite.");
                    return false;
                }

                if (!Registrar.Unregister(protocol))
                {
                    UserInteraction.Error($"{protocol}: Unable to remove existing protocol registration. Try running this program again with administrator privileges.");
                    return false;
                }

                if (Registrar.Register(protocol))
                {
                    UserInteraction.Message($"{protocol}: Successfully registered Sulu as the default protocol handler.");
                }
                else
                {
                    UserInteraction.Error($"{protocol}: Failed to register Sulu as the default protocol handler. Try running this program again with administrator privileges.");
                    return false;
                }
            }
            return true;
        }

        public bool UnRegister(IEnumerable<string> protocols)
        {
            foreach (var protocol in protocols)
            {
                if (Registrar.IsSuluRegistered(protocol))
                {
                    if (!Registrar.Unregister(protocol))
                    {
                        UserInteraction.Error($"{protocol}: Unable to remove Sulu protocol registration. Try running this program again with administrator privileges.");
                        return false;
                    }
                    UserInteraction.Error($"{protocol}: Unregistered Sulu as default URL protocol handler.");
                }
                else
                {
                    UserInteraction.Message($"{protocol}: Sulu does not appear to be registered for this protocol, skipping.");
                }
            }
            return true;
        }
    }
}
