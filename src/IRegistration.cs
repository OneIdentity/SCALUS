using System.Collections.Generic;

namespace scalus
{
    public interface IRegistration
    {
        bool Register(IEnumerable<string> protocols, bool force = false, bool userMode = false, bool useSudo=false);
        bool UnRegister(IEnumerable<string> protocols, bool userMode = false, bool useSudo=false);
        bool IsRegistered(string protocol);
    }
}
