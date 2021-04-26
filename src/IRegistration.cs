using System.Collections.Generic;

namespace scalus
{
    interface IRegistration
    {
        bool Register(IEnumerable<string> protocols, bool force = false, bool userMode = false, bool useSudo=false);
        bool UnRegister(IEnumerable<string> protocols, bool userMode = false, bool useSudo=false);
    }
}
