using System;
using scalus.Dto;
using scalus.Util;
using scalus.Platform;

namespace scalus.Verify
{
    class Application : IApplication
    {
        private Options Options { get; }
        private IScalusConfiguration Configuration { get; }
        public Application(Options options, IScalusConfiguration config )
        {
            Options = options;
            Configuration = config;
        }

        private void ShowConfig()
        {
            var path = string.IsNullOrEmpty(Options.Path) ? ConfigurationManager.ScalusJson : Options.Path;
            ScalusConfig config = Configuration.GetConfiguration(string.IsNullOrEmpty(Options.Path) ? null : Options.Path);
            var ok = (Configuration.ValidationErrors?.Count == 0 ? "Pass" : "Fail");
            Console.WriteLine($"   - Configuration file   : {path}");
            Console.WriteLine($"           Syntax Check   : {ok}");
            Console.WriteLine();

            if (Configuration.ValidationErrors?.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine(" *** Errors:");
                Console.WriteLine();
                foreach (var one in Configuration.ValidationErrors)
                {
                    Console.WriteLine($"        - {one}");
                }
            }
            Console.WriteLine();

        }

     
        public int Run()
        {
            ShowConfig();
            return 0;
        }
    }
}
