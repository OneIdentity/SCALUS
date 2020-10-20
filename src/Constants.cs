using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sulu
{
    static class Constants
    {
        public static string DotNetPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return @"C:\Program Files\dotnet\dotnet.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "/usr/bin/dotnet";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "/usr/local/share/dotnet/dotnet";
            }
            return null;
        }

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
            var binPath = GetBinaryPath().Trim();
            if (binPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || binPath.EndsWith(".so", StringComparison.OrdinalIgnoreCase))
            {
                binPath = $"\"{DotNetPath()}\" \"{binPath}\"";
            }
            else
            {
                binPath = $"\"{binPath}\"";
            }
            return $"{binPath} launch -u \"{urlString}\"";
        }


    }
}
