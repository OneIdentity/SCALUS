namespace scalus.Platform
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

        public bool IsScalusRegistered(string command)
        {
            return false;
        }

        public bool Register(string protocol, bool userMode = false, bool useSudo= false)
        {
            var registrationCommand = Constants.GetLaunchCommand();
            var message = $@"
SCALUS doesn't know how to register a URL protocol handler on this platform.
You can register SCALUS manually using this command: {registrationCommand}
";
            UserInteraction.Message(message);
            return true;
        }

        public bool Unregister(string protocol, bool userMode = false, bool useSudo = false)
        {
            UserInteraction.Message("SCALUS doesn't know how to unregister as a URL protocol handler on this platform.");
            return true;
        }

        public bool ReplaceRegistration(string protocol, bool userMode = false, bool useSudo = false)
        {
            var res =Unregister(protocol, userMode, useSudo);
            if (res)
            {
                res = Register(protocol, userMode, useSudo);
            }
            return res;
        }

    }
}
