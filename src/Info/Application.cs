// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Info
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Newtonsoft.Json;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;

    internal class Application : IApplication
    {
        private readonly IOsServices osServices;

        public Application(Options options, IRegistration registration, IScalusConfiguration config, IOsServices osServices)
        {
            Options = options;
            Registration = registration;
            Configuration = config;
            this.osServices = osServices;
        }

        private Options Options { get; }

        private IRegistration Registration { get; }

        private IScalusConfiguration Configuration { get; }

        public int Run()
        {
            var example = new ScalusConfig()
            {
                Protocols = new List<ProtocolMapping>
                    {
                        { new ProtocolMapping() { Protocol = "myprotocol", AppId = "applicationId" } },
                    },
                Applications = new List<ApplicationConfig>()
                    {
                        new ApplicationConfig()
                        {
                            Id = "applicationId",
                            Name = "applicationName",
                            Description = "optional desc",
                            Protocol = "myprotocol",
                            Platforms = new List<Dto.Platform>
                                { Dto.Platform.Windows, Dto.Platform.Linux, Dto.Platform.Mac },
                            Parser = new ParserConfig()
                            {
                                ParserId = "url",
                                UseDefaultTemplate = false,
                                UseTemplateFile = "/path/tofile",
                                Options = new List<string> { "waitforexit" },
                                PostProcessingExec = "path/toplugin",
                                PostProcessingArgs = new List<string> { "arg1", "arg2" },
                            },
                            Exec = "/path/tocommand",
                            Args = new List<string> { "arg1", "arg2 " },
                        },
                    },
            };

            if (Options.Dto)
            {
                ShowDto();
                return 0;
            }

            if (Options.Tokens)
            {
                ShowTokens();
                return 0;
            }

            ShowConfig();

            return 0;
        }

        private void ShowConfig()
        {
            Console.WriteLine(@"
 * SCALUS CONFIGURATION:
   --------------------
   The 'scalus.json' configuration file describes the supported protocols and the applications 
   (e.g. MS Remote Desktop, FreeRdp, Putty) that will be launched to process URLs for those protocols. 

");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine($"   - Application Path     : {MacOsExtensions.GetAppPath(osServices)}");
            }

            Console.WriteLine($"   - Configuration file   : {ConfigurationManager.ScalusJson}");
            Console.WriteLine($"   - Logfile              : {ConfigurationManager.LogFile}");
            Console.WriteLine();
            Console.WriteLine($"   For detailed information about the scalus configuration file, run info -d");
            Console.WriteLine($"   For detailed information about the tokens that can be used in scalus.json, run info -t");

            var config = Configuration.GetConfiguration();

            Console.WriteLine(@"

   - Protocols currently configured for scalus:
     -----------------------------------------
");
            Console.WriteLine("     {0,-10} {1,-10} {2,-20} {3}", "Protocol", "Registered", "Description", "Configured Command");
            Console.WriteLine("     ------------------------------------------------------------------------------------------------");
            if (config == null || config.Protocols == null || config.Protocols.Count == 0)
            {
                return;
            }

            foreach (var one in config.Protocols)
            {
                if (one == null)
                {
                    continue;
                }

                Console.Write("     {0,-10} ", one.Protocol);
                ApplicationConfig appConfig = null;
                if (config.Applications?.Count > 0)
                {
                    foreach (var a in from a in config.Applications
                                      where a.Id == one.AppId
                                      select a)
                    {
                        appConfig = a;
                        break;
                    }
                }

                if (appConfig != null)
                {
                    Console.Write("{0,-10} {1,-20} ({2} {3})",
                        Registration.IsRegistered(one.Protocol) ? "yes" : "no",
                        appConfig.Description,
                        appConfig.Exec,
                        string.Join(' ', appConfig.Args));
                }
                else
                {
                    Console.Write("{0,-10} {1,-20}",
                        Registration.IsRegistered(one.Protocol) ? "yes" : "no",
                        "Not configured");
                }

                Console.WriteLine();
            }
        }

        private static void ShowDto()
        {
            var example = new ScalusConfig()
            {
                Protocols = new List<ProtocolMapping>
                {
                    { new ProtocolMapping() { Protocol = "myprotocol", AppId = "applicationId" } },
                },
                Applications = new List<ApplicationConfig>()
                {
                    new ApplicationConfig()
                    {
                        Id = "applicationId",
                        Name = "applicationName",
                        Description = "optional desc",
                        Protocol = "myprotocol",
                        Platforms = new List<Dto.Platform>
                            { Dto.Platform.Windows, Dto.Platform.Linux, Dto.Platform.Mac },
                        Parser = new ParserConfig()
                        {
                            ParserId = "url",
                            UseDefaultTemplate = false,
                            UseTemplateFile = "/path/tofile",
                            Options = new List<string> { "waitforexit" },
                            PostProcessingExec = "path/toplugin",
                            PostProcessingArgs = new List<string> { "arg1", "arg2" },
                        },
                        Exec = "/path/tocommand",
                        Args = new List<string> { "arg1", "arg2 " },
                    },
                },
            };

            Console.WriteLine(@"

 * Example SCALUS configuration:
   ----------------------------

");
            Console.WriteLine(JsonConvert.SerializeObject(example, Formatting.Indented));
            Console.WriteLine(@"

 * DTO Properties :
   --------------
");
            foreach (var one in ScalusConfig.DtoPropertyDescription)
            {
                Console.WriteLine($"    - {one.Key} : {one.Value}");
            }
        }

        private static void ShowTokens()
        {
            Console.WriteLine(@"
 - SCALUS Tokens:
   -------------
   The following tokens can be used in the scalus configuration file. 
   Each token will be evaluated and replaced when launching the configured application.
");
            var tokenList = ((string[])Enum.GetNames(typeof(ParserConfigDefinitions.Token))).ToList();
            tokenList.Sort();
            foreach (var one in tokenList)
            {
                Console.WriteLine("   - {0,-10} : {1}",
                    "%" + one + "%",
                    ParserConfigDefinitions.TokenDescription[Enum.Parse<ParserConfigDefinitions.Token>(one, true)]);
            }
        }
    }
}
