using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu.Register
{
    class Application : IApplication
    {
        Register.Options Options { get; }
        public Application(Register.Options options)
        {
            Options = options;
        }

        public int Run()
        {
            Serilog.Log.Debug("Register!");
            return 0;
        }
    }
}
