using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu.Ui
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
            Serilog.Log.Debug("Ui!");
            return 0;
        }
    }
}
