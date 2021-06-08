using System.Collections.Generic;

namespace scalus
{
    public interface IRegistration
    {
        bool Register(IEnumerable<string> protocols, bool force = false, bool rootMode = false, bool useSudo=false);
        bool UnRegister(IEnumerable<string> protocols, bool rootMode = false, bool useSudo=false);
        bool IsRegistered(string protocol, bool useSudo = false);
    }
}
