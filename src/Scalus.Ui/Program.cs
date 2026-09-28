// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Ui
{
    using System;
    using System.IO;
    using OneIdentity.Scalus.Util;
    using Photino.NET;
    using Serilog;

    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            // When the OS invokes this executable as a protocol handler it passes a verb such as
            // "launch -u rdp://...". In that case we must run headless through the shared CLI
            // pipeline (spawning the native session client) instead of opening the config window.
            // A bare invocation, or one carrying only the UI-only "--show-logs" deep link, opens
            // the desktop UI.
            var showLogs = TryGetShowLogs(args);
            if (showLogs == null && args.Length > 0)
            {
                return CommandLineRunner.Run(args);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(ConfigurationManager.MinLogLevel ?? Serilog.Events.LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    ConfigurationManager.UiLogFile,
                    outputTemplate: ConfigurationManager.LogOutputTemplate,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: ConfigurationManager.LogRetainedFileCountLimit,
                    shared: true)
                .CreateLogger();

            try
            {
                Run(showLogs);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SCALUS configuration app terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }

            return 0;
        }

        // Recognizes the UI-only "--show-logs=<launchId>" (or "--show-logs <launchId>") deep link so
        // it is not mistaken for a CLI verb. Returns the launch id ("" when the flag has no value),
        // or null when the flag is absent.
        private static string TryGetShowLogs(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith("--show-logs=", StringComparison.Ordinal))
                {
                    return arg.Substring("--show-logs=".Length);
                }

                if (string.Equals(arg, "--show-logs", StringComparison.Ordinal))
                {
                    return i + 1 < args.Length ? args[i + 1] : string.Empty;
                }
            }

            return null;
        }

        private static void Run(string showLogs)
        {
            var baseDir = AppContext.BaseDirectory;
            var indexPath = Path.Combine(baseDir, "wwwroot", "index.html");
            var iconPath = Path.Combine(baseDir, "scalus.ico");
            var windowSettingsPath = Path.Combine(ConfigurationManager.ProdAppPath, "ui-window.json");
            var windowSettings = UiWindowSettings.Load(windowSettingsPath);
            var restoredWidth = windowSettings.Width;
            var restoredHeight = windowSettings.Height;
            var container = Ioc.RegisterApplication(Log.Logger);
            var dispatcher = new BridgeDispatcher(container, showLogs);

            Log.Information("Starting SCALUS configuration app ({IndexPath})", indexPath);

            var window = new PhotinoWindow()
                .SetTitle("SCALUS")
                .SetUseOsDefaultSize(false)
                .SetSize(windowSettings.Width, windowSettings.Height)
                .SetMinSize(UiWindowSettings.MinimumWidth, UiWindowSettings.MinimumHeight)
                .Center()
                .SetContextMenuEnabled(false)
                .SetDevToolsEnabled(true)
                .SetMaximized(windowSettings.Maximized);

            if (File.Exists(iconPath))
            {
                window.SetIconFile(iconPath);
            }

            dispatcher.Attach(window);
            window.RegisterWebMessageReceivedHandler((sender, message) =>
            {
                var self = (PhotinoWindow)sender;
                var response = dispatcher.Dispatch(message);
                self.SendWebMessage(response);
            });
            window.RegisterSizeChangedHandler((sender, size) =>
            {
                var self = (PhotinoWindow)sender;
                if (!self.Maximized)
                {
                    restoredWidth = size.Width;
                    restoredHeight = size.Height;
                }
            });
            window.RegisterWindowClosingHandler((sender, _) =>
            {
                var self = (PhotinoWindow)sender;
                UiWindowSettings.Save(windowSettingsPath, restoredWidth, restoredHeight, self.Maximized);
                return false;
            });

            window.Load(indexPath);
            window.WaitForClose();
        }
    }
}
