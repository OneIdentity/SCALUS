using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OneIdentity.Scalus.Prelaunch
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            // Launch splash screen thread right away
#if COMMUNITY_EDITION
            var splashResource = "Splash-CommunityScalus.png";
#else
            var splashResource = "Splash-SafeguardScalus.png";
#endif
            var splash = new System.Windows.SplashScreen(splashResource);
            splash.Show(false, false);

            // Launch target app and wait for it to start
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyPath = Path.GetDirectoryName(assembly.Location);
            var name = Path.GetFileName(assembly.Location);
            var target = Path.Combine(assemblyPath, "_" + name);

            // Create a named semaphore
            using (var sem = new Semaphore(0, 1, "OneIdentity.Scalus", out bool created))
            {

                if (!created)
                {
                    MessageBox.Show("Failed to create semaphore. Bailing out...", "SCALUS Launcher");
                    return 1;
                }

                // Start the process and pass the args through
                var processStartInfo = new ProcessStartInfo(target, string.Join(" ", args))
                {
                    ErrorDialog = true,
                };

                using (Process.Start(processStartInfo))
                {
                    sem.WaitOne(TimeSpan.FromSeconds(15));
                }

                return 0;
            }
        }
    }
}
