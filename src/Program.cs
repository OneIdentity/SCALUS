// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="One Identity Inc.">
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

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Autofac;
using CommandLine;
using OneIdentity.Scalus.Util;
using Serilog;
using Serilog.Core;

[assembly: InternalsVisibleTo("scalus.Test")]

namespace OneIdentity.Scalus
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            ConfigureLogging();
            CheckConfig();
            try
            {
                // Register components with autofac
                using var container = Ioc.RegisterApplication(Log.Logger);
                using var lifetimeScope = container.BeginLifetimeScope();

                // Resolve the command line parser and 
                // resolve a corresponding application instance
                var parser = lifetimeScope.Resolve<ICommandLineParser>();
                var application = parser.Build(args, x =>
                {
                    var type = x.GetType();
                    var verb = type?.GetCustomAttribute<VerbAttribute>()?.Name;
                    return verb == null ? null : lifetimeScope.ResolveNamed<IApplication>(verb, new TypedParameter(type, x));
                });

                // If application is null, then they ran help or version commands, just return
                if (application == null) return 0;

                // Run application
                return application.Run();
            }
            catch (CommandLineHelpException ex)
            {
                // Command line usage
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                HandleUnexpectedError(ex);
            }

            return 1;
        }

        private static void HandleUnexpectedError(Exception ex)
        {
            Serilog.Log.Error($"Unexpected error: {ex.Message}", ex);
            string indent = "  ";
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                Serilog.Log.Error($"{indent}=> Inner Exception: {ex.Message}", ex);
                indent += "  ";
            }
        }


        private static void CheckConfig()
        {
            Log.Logger.Information($"CheckConfig");
            if (File.Exists(ConfigurationManager.ScalusJson))
            {
                Log.Logger.Information($"ok");
                return;
            }

            var defpath = ConfigurationManager.ScalusJsonDefault;
            if (!File.Exists(defpath))
            {
                Log.Logger.Warning($"Config file not found:{ConfigurationManager.ScalusJson} and installed default file not found:{defpath}");
                return;
            }

            try
            {
                var dir = Path.GetDirectoryName(ConfigurationManager.ScalusJson);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                Log.Logger.Information($"Initializing config file:{ConfigurationManager.ScalusJson} from the installed file:{defpath}");
                File.WriteAllText(ConfigurationManager.ScalusJson, File.ReadAllText(defpath));

                var egs = ConfigurationManager.ExamplePath;
                if (Directory.Exists(egs))
                {
                    var files = Directory.EnumerateFiles(egs);
                    foreach (var one in files)
                    {
                        var to = Path.Combine(ConfigurationManager.ProdAppPath, Path.GetFileName(one));
                        if (File.Exists(one) && !File.Exists(to))
                        {
                            File.WriteAllText(to, File.ReadAllText(one));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Information($"Failed to initialize config file:{ConfigurationManager.ScalusJson} from installed file:{defpath}: {e.Message}");
            }
        }

        private static void ConfigureLogging()
        {
            var logFilePath = ConfigurationManager.LogFile;
            var config = new LoggerConfiguration();
            config.WriteTo.File(logFilePath, shared: true);
            if (ConfigurationManager.MinLogLevel != null)
            {
                config.MinimumLevel.ControlledBy(new LoggingLevelSwitch(ConfigurationManager.MinLogLevel.Value));
            }

            if (ConfigurationManager.LogToConsole)
            {
                config.WriteTo.Console();
            }

            Log.Logger = config.CreateLogger();
        }
    }
}
