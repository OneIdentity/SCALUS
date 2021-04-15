using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace scalus
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
                var path = "/usr/bin/dotnet";
                if(!File.Exists(path))
                {
                    Serilog.Log.Warning($"dotnet not found at: {path}");
                }
                return path;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "/usr/local/share/dotnet/dotnet";
            }
            return null;
        }
        public static string GetBinaryName()
        {
            return Path.GetFileName(GetBinaryPath());
        }
        public static string GetBinaryPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Process.GetCurrentProcess().MainModule.FileName;
            }
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
            var cmd = $"{binPath} launch -u \"{urlString}\"";
            Serilog.Log.Information($"Launch cmd:{cmd}");
            return cmd;
        }
    }
}
