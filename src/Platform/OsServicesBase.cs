// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OsServicesBase.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.Principal;
    using OneIdentity.Scalus.Util;
    using Serilog;

    public partial class OsServicesBase : IOsServices
    {
        public object OsServices { get; private set; }

        public Process OpenDefault(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Process.Start("open", url);
            }

            Log.Information($"unknown platform {RuntimeInformation.OSDescription} - can't prompt");
            return null;
        }

        public Process Execute(string command, IEnumerable<string> args)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new PlatformException("Missing command");
            }

            var startupInfo = new ProcessStartInfo(command)
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal,
            };
            foreach (var arg in args)
            {
                startupInfo.ArgumentList.Add(arg.Trim());
            }

            Log.Logger.Information($"Running process: {command} with args: {string.Join(' ', args)}");
            var process = Process.Start(startupInfo);
            Log.Logger.Information($"Started process, id: {process?.Id}, exited: {process?.HasExited}");
            return process;
        }

        // execute a command, wait for it to end, return the exit code and retrieve the stdout & stderr
        public int Execute(string command, IEnumerable<string> args, out string stdOut, out string stdErr)
        {
            stdOut = string.Empty;
            stdErr = string.Empty;
            if (string.IsNullOrEmpty(command))
            {
                throw new PlatformException("missing command");
            }

            Log.Logger.Information($"Running: {command}, args: {string.Join(", ", args)}");
            var startupInfo = new ProcessStartInfo(command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            if (args != null)
            {
                foreach (var arg in args)
                {
                    startupInfo.ArgumentList.Add(arg);
                }
            }

            var process = Process.Start(startupInfo);
            if (process == null)
            {
                throw new PlatformException($"Failed to run: {command}");
            }

            process.WaitForExit();
            stdOut = process.StandardOutput.ReadToEnd();
            stdErr = process.StandardError.ReadToEnd();
            Log.Logger.Information($"Result: {process.ExitCode}, output: {stdOut}, err: {stdErr}");
            return process.ExitCode;
        }

        public bool IsAdministrator()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                bool isAdmin;
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }

                return isAdmin;
            }

            return geteuid() == 0;
        }

        public void ShowLaunchFailure(string message, string launchId)
        {
            var body = string.IsNullOrWhiteSpace(message) ? "SCALUS could not launch the session." : message;
            bool openLogs;
            try
            {
                openLogs = PromptOpenLogs(body);
            }
            catch (Exception ex)
            {
                // A dialog helper misbehaving must never mask the original launch failure.
                Log.Error(ex, "Failed to show the launch-failure dialog");
                return;
            }

            if (openLogs)
            {
                SpawnUiShowLogs(launchId);
            }
        }

        public void ShowMessage(string message)
        {
            Log.Information(message);
        }

        // Shows a modal asking whether to open the SCALUS logs. Returns true only when the user
        // explicitly chooses to open them. On a headless host (or with no dialog helper available)
        // it logs and returns false so the launch failure is never silently swallowed.
        private static bool PromptOpenLogs(string message)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return PromptOpenLogsWindows(message);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // AppleScript string literals don't accept raw newlines; collapse them for the prompt.
                var oneLine = message.Replace("\r\n", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
                var script =
                    "display dialog " + AppleScriptLiteral(oneLine) +
                    " with title \"SCALUS\" with icon caution buttons {\"Close\", \"Open Logs\"} default button \"Open Logs\"";
                var code = TryRun("osascript", new[] { "-e", script }, out var stdOut, out _);
                return code == 0 && stdOut.Contains("Open Logs", StringComparison.Ordinal);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var hasDisplay = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))
                    || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
                if (!hasDisplay)
                {
                    Log.Error("Launch failed (no display for a dialog): {Message}", message);
                    return false;
                }

                if (TryRun("zenity", new[] { "--question", "--title=SCALUS", "--text=" + message, "--ok-label=Open Logs", "--cancel-label=Close" }, out _, out _, out var zenityRan) && zenityRan)
                {
                    return true;
                }

                if (zenityRan)
                {
                    return false;
                }

                if (TryRun("kdialog", new[] { "--title", "SCALUS", "--yesno", message, "--yes-label", "Open Logs", "--no-label", "Close" }, out _, out _, out var kdialogRan) && kdialogRan)
                {
                    return true;
                }

                if (!kdialogRan)
                {
                    Log.Error("Launch failed (no zenity/kdialog available): {Message}", message);
                }

                return false;
            }

            Log.Error("Launch failed (unsupported platform for a dialog): {Message}", message);
            return false;
        }

        [SupportedOSPlatform("windows")]
        private static bool PromptOpenLogsWindows(string message)
        {
            const uint MB_YESNO = 0x00000004;
            const uint MB_ICONERROR = 0x00000010;
            const uint MB_SETFOREGROUND = 0x00010000;
            const int IDYES = 6;
            var text = message + "\n\nOpen the SCALUS logs to see what happened?";
            var result = MessageBoxW(IntPtr.Zero, text, "SCALUS", MB_YESNO | MB_ICONERROR | MB_SETFOREGROUND);
            return result == IDYES;
        }

        // Launches the desktop UI deep-linked to the failed launch. When the UI binary can't be
        // found we fall back to opening the logs folder so the records are still reachable.
        private void SpawnUiShowLogs(string launchId)
        {
            try
            {
                var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "scalus-ui.exe" : "scalus-ui";
                var uiPath = Path.Combine(AppContext.BaseDirectory, exe);
                if (File.Exists(uiPath))
                {
                    Process.Start(new ProcessStartInfo(uiPath, $"--show-logs={launchId}") { UseShellExecute = false });
                    return;
                }

                Log.Warning("Desktop UI binary not found at {UiPath}; opening the logs folder instead", uiPath);
                OpenDefault(ConfigurationManager.LogDir);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open the SCALUS logs for launch {LaunchId}", launchId);
            }
        }

        // Runs a helper process to completion, returning its exit code. Throws nothing the caller
        // needs to handle for "helper not installed" — that surfaces as ran=false.
        private static int TryRun(string command, IReadOnlyList<string> args, out string stdOut, out string stdErr)
            => TryRun(command, args, out stdOut, out stdErr, out _) ? 0 : -1;

        private static bool TryRun(string command, IReadOnlyList<string> args, out string stdOut, out string stdErr, out bool ran)
        {
            stdOut = string.Empty;
            stdErr = string.Empty;
            ran = false;
            try
            {
                var startupInfo = new ProcessStartInfo(command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };
                foreach (var arg in args)
                {
                    startupInfo.ArgumentList.Add(arg);
                }

                var process = Process.Start(startupInfo);
                if (process == null)
                {
                    return false;
                }

                ran = true;
                process.WaitForExit();
                stdOut = process.StandardOutput.ReadToEnd();
                stdErr = process.StandardError.ReadToEnd();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                // Most commonly the helper (osascript/zenity/kdialog) isn't installed.
                Log.Debug("Dialog helper '{Command}' unavailable: {Message}", command, ex.Message);
                return false;
            }
        }

        // Escapes a string for embedding inside an AppleScript double-quoted literal.
        private static string AppleScriptLiteral(string value)
            => "\"" + (value ?? string.Empty).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

        [SupportedOSPlatform("windows")]
        [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        [LibraryImport("libc")]
        private static partial uint geteuid();
    }
}
