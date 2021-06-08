using System.Collections.Generic;

namespace scalus
{
    interface IProtocolRegistrar
    {
        bool UseSudo { get; set; }
        bool RootMode { get; set; }
        string Name { get; }
        string GetRegisteredCommand(string protocol);
        bool IsScalusRegistered(string command);
        bool Unregister(string protocol);
        bool Register(string protocol);
        bool ReplaceRegistration(string protocol);
    }
}
