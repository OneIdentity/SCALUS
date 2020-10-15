using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu.Launch
{
    class Application : IApplication
    {
        Launch.Options Options { get; }
        public Application(Launch.Options options)
        {
            this.Options = options;
        }

        public int Run()
        {
            Serilog.Log.Debug($"Launch: {Options.Url}");
            return 0;
        }
    }
}
