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
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Serilog;

    public class OsServicesBase : IOsServices
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

            Log.Information($"unknown platform {RuntimeInformation.OSDescription}- cant prompt");
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

            Log.Logger.Information($"Running process:{command} with args:{string.Join(' ', args)}");
            var process = Process.Start(startupInfo);
            Log.Logger.Information($"Started process, id:{process?.Id}, exited:{process?.HasExited}");
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

            Log.Logger.Information($"Running:{command}, args:{string.Join(',', args)}");
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
                throw new PlatformException($"Failed to run:{command}");
            }

            process.WaitForExit();
            stdOut = process.StandardOutput.ReadToEnd();
            stdErr = process.StandardError.ReadToEnd();
            Log.Logger.Information($"Result:{process.ExitCode}, output:{stdOut}, err:{stdErr}");
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

        public void OpenText(string message)
        {
            var tempFile = GetTempFile(".txt");
            try
            {
                File.WriteAllText(tempFile, message);
                var l = File.ReadAllText(tempFile);
                var process = OpenDefault(tempFile);
                if (process == null)
                {
                    Log.Error($"Failed to report warning:{message}");
                }
                else
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        Task.Delay(10 * 1000).Wait();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error($"Failed launching URL: {ex.Message}", ex);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        private static string GetTempFile(string ext)
        {
            var tempFile = Path.GetTempFileName();
            string renamed = Path.ChangeExtension(tempFile, ext);
            File.Move(tempFile, renamed);
            return renamed;
        }

        [DllImport("libc")]
        private static extern uint geteuid();
    }
}
