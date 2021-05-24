using System;
using System.Collections.Generic;
using scalus.Dto;
using scalus.Util;
using System.Linq;
using Newtonsoft.Json;

namespace scalus.Info
{
    class Application : IApplication
    {
        private Options Options { get; }
        private IRegistration Registration { get; }
        private IScalusConfiguration Configuration { get; }
        public Application(Options options, IRegistration registration, IScalusConfiguration config)
        {
            Options = options;
            Registration = registration;
            Configuration = config;
        }

        public int Run()
        {
            if (Options.Dto)
            {
                var example = new ScalusConfig()
                {
                    Applications = new List<ApplicationConfig>( )
                    {
                        new ApplicationConfig()
                        {
                            Id = "id",
                            Name = "name",
                            Description = "desc",
                            Protocol = "myprotocol",
                            Platforms = new List<string>{ "windows", "unix", "mac"},
                            Parser = new ParserConfig()
                            {
                                Id = "url",
                                UseDefaultTemplate = false,
                                UseTemplateFile = "/path/tofile",
                                Options = new List<string> { "waitforexit" },
                                PostProcessingExec = "path/toplugin",
                                PostProcessingArgs = new List<string> { "arg1", "arg2"},
                            },
                            Exec = "/path/tocommand",
                            Args = new List<string> { "arg1", "arg2 " }
                        }
                    }
                };
                Console.WriteLine(@"
SCALUS CONFIGURATION:
--------------------
The 'scalus.json' configuration file describes the supported protocols and the applications that will be launched to process them (e.g. Microsoft Remote Desktop, FreeRDP) that will be launched to handle those URLS.

");
                Console.WriteLine($"Example scalus configuration: ");
                Console.WriteLine();
                Console.WriteLine(JsonConvert.SerializeObject(example));
                Console.WriteLine(@"

For more detailed information about a DTO property, run info -p <property>

");
                Console.WriteLine(@"
SCALUS Tokens:
-------------
The following tokens can be used in the scalus configuration. Each token will be evaluated and replaced when launching the configured application.
");
                foreach (var one in Enum.GetValues(typeof(ParserConfigDefinitions.Token)))
                {
                    Console.WriteLine("{0,-10} : {1}", one, ParserConfigDefinitions.TokenDescription[(ParserConfigDefinitions.Token)one]);
                }
                return 0;
            }

            if (!string.IsNullOrEmpty(Options.Property))
            {
                Console.WriteLine(" - DTO Property:");
                if (!string.IsNullOrEmpty(Options.Property))
                {
                    if (ScalusConfig.DtoPropertyDescription.ContainsKey(Options.Property))
                    {
                        Console.WriteLine($" {Options.Property} : {ScalusConfig.DtoPropertyDescription[Options.Property]}");
                        return 0;
                    }

                    Console.WriteLine($" Property not found:{Options.Property}");
                    return 1;
                }
            }
            Console.WriteLine(@"

SCALUS configuration :
--------------------

");
            Console.WriteLine($" - Configuration file   : {ConfigurationManager.ScalusJson}");
            Console.WriteLine($" - Logfile              : {ConfigurationManager.LogFile}" );
            var config = Configuration.GetConfiguration();
            Console.WriteLine(@"
- Protocols configured for scalus:
  -------------------------------
");
            Console.WriteLine("   {0,-10} {1,-10} {2,-20} {3}", "Protocol", "Registered", "Description", "Command");
            Console.WriteLine("-------------------------------------------------");
            foreach (var one in config.Protocols)
            {
                Console.Write("   {0,-10} ", one.Protocol);
                ApplicationConfig appConfig = null;
                foreach (var a in from a in config.Applications
                    where a.Id == one.AppId
                    select a)
                {
                    appConfig = a;
                    break;
                }
                if (appConfig != null)
                {
                    Console.Write("{0,-10} {1,-20} ({2} {3})",  Registration.IsRegistered(one.Protocol)?"yes":"no",appConfig.Description, appConfig.Exec, string.Join(' ', appConfig.Args));
                }
                Console.WriteLine();
            }

            Console.WriteLine();

            return 0;
        }
    }
}
