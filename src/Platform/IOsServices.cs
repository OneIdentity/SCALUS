using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu.Platform
{
    public interface IOsServices
    {
        Process OpenDefault(string file);

        Process Execute(string binary, string args);

        bool IsAdministrator();
    }
}
