using System.Collections.Generic;
using scalus.Platform;

namespace scalus
{
    public interface IProtocolRegistrar
    {
        IOsServices OsServices { get; }
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
