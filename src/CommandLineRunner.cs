// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandLineRunner.cs" company="One Identity Inc.">
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
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;
    using OneIdentity.Scalus.Util;
    using Serilog;
    using Serilog.Sinks.SystemConsole.Themes;

    /// <summary>
    /// Shared command-line pipeline used by both the CLI executable (scalus) and the
    /// desktop UI executable (scalus-ui) when it is invoked as a protocol handler
    /// (e.g. "scalus-ui launch -u rdp://..."). Both front-ends delegate here so that
    /// verb parsing, configuration bootstrap, and headless execution behave identically
    /// regardless of which binary the OS invoked to handle the URL.
    /// </summary>
    public static class CommandLineRunner
    {
        /// <summary>
        /// Parses the supplied command-line arguments, resolves the matching verb, and runs it
        /// headless. Returns the process exit code. Never opens a GUI window.
        /// </summary>
        public static int Run(string[] args)
        {
            ConfigureLogging();
            if (!IsMachineUnregister(args))
            {
                CheckConfig();
            }

            try
            {
                var logger = new LoggerConfiguration().WriteTo.Console(theme: ConsoleTheme.None).CreateLogger();
                using var provider = Ioc.RegisterApplication(logger);

                var parser = provider.GetRequiredService<ICommandLineParser>();
                var application = parser.Build(args, x => Ioc.CreateVerbApplication(provider, x), out var exitCode);

                // If application is null, then they ran help, version, or an invalid command
                if (application == null)
                {
                    ReleaseLaunchSemaphore();
                    return exitCode;
                }

                ReleaseLaunchSemaphore();
                return application.Run();
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
                Log.Debug("Failed to release semaphore!");

                // Failed to open the semaphore, so we can't signal it.
                // The launcher will time out after 15 seconds.
            }
        }

        private static bool IsMachineUnregister(string[] args) =>
            args != null &&
            args.Any(arg => string.Equals(arg, "unregister", StringComparison.OrdinalIgnoreCase)) &&
            args.Any(arg => string.Equals(arg, "--root", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-r", StringComparison.OrdinalIgnoreCase));

        private static void HandleUnexpectedError(Exception ex)
        {
            Log.Error($"Unexpected error: {ex.Message}", ex);
            string indent = "  ";
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                Log.Error($"{indent}=> Inner Exception: {ex.Message}", ex);
                indent += "  ";
            }
        }

        private static void CheckConfig()
        {
            Log.Logger.Information($"CheckConfig");
            if (File.Exists(ConfigurationManager.ScalusJson))
            {
                Log.Logger.Information($"ok");
                try
                {
                    new ScalusApiConfiguration().MigrateOnDisk();
                }
                catch (Exception e)
                {
                    Log.Logger.Warning($"Template migration check failed: {e.Message}");
                }

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

                // The shipped default lists applications for every platform; seed only the ones
                // valid on this OS so a fresh config never offers, e.g., a macOS client on Windows.
                // If the default can't be parsed for any reason, fall back to copying it verbatim
                // so a malformed-but-usable default still bootstraps a working config.
                var defJson = File.ReadAllText(defpath);
                try
                {
                    var cfg = ScalusJson.Deserialize(defJson);
                    if (cfg?.Applications != null)
                    {
                        PlatformFilter.ApplyToCurrentPlatform(cfg);
                        defJson = ScalusJson.Serialize(cfg);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning($"Could not platform-filter the default config, seeding it verbatim: {ex.Message}");
                }

                File.WriteAllText(ConfigurationManager.ScalusJson, defJson);

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
            var config = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    ConfigurationManager.LauncherLogFile,
                    outputTemplate: ConfigurationManager.LogOutputTemplate,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: ConfigurationManager.LogRetainedFileCountLimit,
                    shared: true);
            if (ConfigurationManager.MinLogLevel != null)
            {
                config.MinimumLevel.ControlledBy(new Serilog.Core.LoggingLevelSwitch(ConfigurationManager.MinLogLevel.Value));
            }

            if (ConfigurationManager.LogToConsole)
            {
                config.WriteTo.Console(outputTemplate: ConfigurationManager.LogOutputTemplate);
            }

            Log.Logger = config.CreateLogger();
        }
    }
}
