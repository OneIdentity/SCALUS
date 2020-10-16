using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu.Platform
{
    class UnsupportedPlatformRegistrar : IProtocolRegistrar
    {
        IUserInteraction UserInteraction { get; }
        public UnsupportedPlatformRegistrar(IUserInteraction userInteraction)
        {
            UserInteraction = userInteraction;
        }

        public string GetRegisteredCommand(string protocol)
        {
            return null;
        }

        public bool IsSuluRegistered(string command)
        {
            return false;
        }

        public bool Register(string protocol)
        {
            var registrationCommand = Constants.GetLaunchCommand();
            var message = $@"
Sulu doesn't know how to register as a URL protocol handler on this platform.
You can register Sulu manually using this command: {registrationCommand}
";
            UserInteraction.Message(message);
            return true;
        }

        public bool Unregister(string protocol)
        {
            UserInteraction.Message("Sulu doesn't know how to unregister as a URL protocol handler on this platform.");
            return true;
        }
    }
}
