using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu.Unregister
{
    class Application : IApplication
    {
        Options Options { get; }

        public Application(Options options)
        {
            Options = options;
        }

        public int Run()
        {
            Serilog.Log.Debug("Unregister!");
            return 0;
        }
    }
}
