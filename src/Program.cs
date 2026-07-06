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

namespace OneIdentity.Scalus
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;
    using Autofac;
    using CommandLine;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;
    using Serilog;
    using Serilog.Sinks.SystemConsole.Themes;

    internal class Program
    {
        private static bool consoleHidden;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        private static int Main(string[] args)
        {
            // On Windows, detach from the console for 'launch' and 'ui' verbs so no console
            // window is visible. When launched from a browser/Explorer, the console window
            // (which was created just for this process) is destroyed. (GitHub issue #131)
            // CLI commands (info, register, --help, etc.) keep the console as normal.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ShouldHideConsole(args))
            {
                FreeConsole();
                consoleHidden = true;

                // Redirect stdout/stderr to nowhere so that subsequent Console.WriteLine
                // and Serilog console sinks don't throw on the invalid handle.
                Console.SetOut(TextWriter.Null);
                Console.SetError(TextWriter.Null);
            }

            bool community = false;
#if COMMUNITY_EDITION
            community = true;
#endif

            Console.WriteLine(community ? "Community Edition" : "Safeguard Edition");
            ConfigureLogging();
            CheckConfig();
            IOsServices services = null;
            try
            {
                // Register components with autofac
                var logConfig = new LoggerConfiguration().WriteTo.File(ConfigurationManager.LogFile, shared: true);
                if (!consoleHidden)
                {
                    logConfig.WriteTo.Console(theme: ConsoleTheme.None);
                }

                var logger = logConfig.CreateLogger();
                using var container = Ioc.RegisterApplication(logger);
                using var lifetimeScope = container.BeginLifetimeScope();
                services = lifetimeScope.Resolve<IOsServices>();

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
                if (application == null)
                {
                    ReleaseLaunchSemaphore();
                    return 0;
                }

                // Run application
                ReleaseLaunchSemaphore();
                return application.Run();
            }
            catch (CommandLineHelpException ex)
            {
                // Command line usage
                if (services != null)
                {
                    services.ShowMessage(ex.Message);
                }
                else
                {
                    Serilog.Log.Error(ex.Message);
                }
            }
            catch (Exception ex)
            {
                HandleUnexpectedError(ex);
            }

            return 1;
        }

        private static void ReleaseLaunchSemaphore()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                using var sem = Semaphore.OpenExisting("OneIdentity.Scalus");
                sem.Release();
            }
            catch (Exception)
            {
                Serilog.Log.Debug("Failed to release semaphore!");
                // Failed to open the semaphore, so we can't signal it.
                // The launcher will time out after 15 seconds.
            }
        }

        private static bool ShouldHideConsole(string[] args)
        {
            if (args.Length == 0)
            {
                // No verb defaults to 'ui'
                return true;
            }

            // --debug or -p/--preview flags keep the console visible for troubleshooting
            if (args.Any(a => string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(a, "--preview", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(a, "-p", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Don't hide the console when the user explicitly requests help/version output.
            if (args.Any(a =>
                    string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "--version", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "-v", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return string.Equals(args[0], "launch", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(args[0], "ui", StringComparison.OrdinalIgnoreCase);
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
            Serilog.Log.Logger.Information($"CheckConfig");
            if (File.Exists(ConfigurationManager.ScalusJson))
            {
                Serilog.Log.Logger.Information($"ok");
                return;
            }

            var defpath = ConfigurationManager.ScalusJsonDefault;
            if (!File.Exists(defpath))
            {
                Serilog.Log.Logger.Warning($"Config file not found:{ConfigurationManager.ScalusJson} and installed default file not found:{defpath}");
                return;
            }

            try
            {
                var dir = Path.GetDirectoryName(ConfigurationManager.ScalusJson);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                Serilog.Log.Logger.Information($"Initializing config file:{ConfigurationManager.ScalusJson} from the installed file:{defpath}");
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
                Serilog.Log.Logger.Information($"Failed to initialize config file:{ConfigurationManager.ScalusJson} from installed file:{defpath}: {e.Message}");
            }
        }

        private static void ConfigureLogging()
        {
            var logFilePath = ConfigurationManager.LogFile;
            var config = new Serilog.LoggerConfiguration();
            config.WriteTo.File(logFilePath, shared: true);
            if (ConfigurationManager.MinLogLevel != null)
            {
                config.MinimumLevel.ControlledBy(new Serilog.Core.LoggingLevelSwitch(ConfigurationManager.MinLogLevel.Value));
            }

            if (!consoleHidden && ConfigurationManager.LogToConsole)
            {
                config.WriteTo.Console();
            }

            Serilog.Log.Logger = config.CreateLogger();
        }
    }
}
