using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Sulu.Platform
{
    

    public class OsServicesBase : IOsServices
    {
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
            else
            {
                return geteuid() == 0;
            }
        }
    }
}
