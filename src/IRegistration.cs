using System.Collections.Generic;

namespace Sulu
{
    interface IRegistration
    {
        bool Register(IEnumerable<string> protocols, bool force = false);
        bool UnRegister(IEnumerable<string> protocols);
    }
}
