using System;
using System.Linq;

namespace Sulu
{
    static class Constants
    {
        public static string GetBinaryPath()
        {
            return Environment.GetCommandLineArgs().First();
        }

        public static string GetBinaryDir()
        {
            return AppContext.BaseDirectory;
        }

        public static string GetLaunchCommand(string urlString = "<URL VARIABLE HERE>")
        {
            return $"\"{GetBinaryPath()}\" launch -u \"{urlString}\"";
        }
    }
}
