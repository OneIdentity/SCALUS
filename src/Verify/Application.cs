using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using scalus.Dto;
using scalus.Util;
using scalus.Platform;

namespace scalus.Verify
{
    public class VerifyResult
    {
        public string Path { get; set; }
        public bool Result { get; set; }
        public List<string> Errors { get; set; }
    }
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
            ScalusConfig config = Configuration.GetConfiguration(
                string.IsNullOrEmpty(Options.Path) ? null : Options.Path);

            var ok = (Configuration.ValidationErrors?.Count == 0 ? "Pass" : "Fail");
            var result = new VerifyResult
            {
                Path = (string.IsNullOrEmpty(Options.Path) ? ConfigurationManager.ScalusJson : Options.Path),
                Result = (Configuration.ValidationErrors?.Count == 0),
                Errors = Configuration.ValidationErrors
            };
            var json = JsonConvert.SerializeObject(result,Formatting.Indented );
            Console.WriteLine(json);
        }

     
        public int Run()
        {
            ShowConfig();
            return 0;
        }
    }
}
