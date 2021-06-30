using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Serilog;

namespace scalus.Platform
{
    public class OsServicesBase : IOsServices
    {
        public object OsServices { get; private set; }

        [DllImport("libc")]
        public static extern uint geteuid();

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
                throw new Exception("Missing command");
            }
            var startupInfo = new ProcessStartInfo(command)
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal
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
        //execute a command, wait for it to end, return the exit code and retrieve the stdout & stderr
        public int Execute(string command, IEnumerable<string> args, out string stdOut, out string stdErr)
        {
            stdOut = string.Empty;
            stdErr = string.Empty;
            if (string.IsNullOrEmpty(command))
            {
                throw new Exception("missing command");
            }
            Log.Logger.Information($"Running:{command}, args:{string.Join(',', args)}");
            var startupInfo = new ProcessStartInfo(command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
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
                throw new Exception($"Failed to run:{command}");
            }

            process.WaitForExit();
            stdOut = process.StandardOutput.ReadToEnd() ;
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

        private string GetTempFile(string ext)
        {
            var tempFile = Path.GetTempFileName();
            string renamed = Path.ChangeExtension(tempFile, ext);
            File.Move(tempFile, renamed);
            return renamed;
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
                else {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        Task.Delay(10* 1000).Wait();
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
    }
}
