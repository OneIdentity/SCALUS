using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

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
            Serilog.Log.Information($"unknown platform {RuntimeInformation.OSDescription}- cant prompt");
            return null;
        }

        public Process Execute(string command, IEnumerable<string> args)
        {
            var startupInfo = new ProcessStartInfo(command);
            foreach (var arg in args)
            {
                startupInfo.ArgumentList.Add(arg);
            }
            return Process.Start(startupInfo);
        }

        public Process Execute(string command, IEnumerable<string> args, out string stdOut, out string stdErr)
        {
            stdOut = string.Empty;
            stdErr = string.Empty;
            var startupInfo = new ProcessStartInfo(command);
            foreach (var arg in args)
            {
                startupInfo.ArgumentList.Add(arg);
            }
            startupInfo.RedirectStandardOutput = true;
            startupInfo.RedirectStandardError = true;
            return Process.Start(startupInfo);
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
            Serilog.Log.Information($"SHOUT - tmp:{tempFile}, rename:{renamed}");
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.WaitForExit();
                }
                else
                {
                    Task.Delay(10* 1000).Wait();
                }
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error($"Failed launching URL: {ex.Message}", ex);
            }
            finally
            {
                Serilog.Log.Information($"SHOUT - deleting :{tempFile}");
                File.Delete(tempFile);
            }
        }
    }
}
