using System.Collections.Generic;

namespace scalus
{
    interface IProtocolRegistrar
    {
        string GetRegisteredCommand(string protocol);
        bool IsScalusRegistered(string command);
        bool Unregister(string protocol, bool userMode=false, bool useSudo = false);
        bool Register(string protocol, bool userMode = false, bool useSudo = false);
        bool ReplaceRegistration(string protocol, bool userMode = false, bool useSudo = false);
    }
}
