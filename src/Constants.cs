// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.Versioning.SupportedOSPlatform("osx")]
    internal static class Constants
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
                if (!File.Exists(path))
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

        public static string GetBinaryDirectory()
        {
            return Path.GetDirectoryName(GetBinaryPath());
        }

        public static string GetBinaryPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var str = Environment.ProcessPath;
                return str;
            }

            return Environment.GetCommandLineArgs().First();
        }

        public static string GetBinaryDir()
        {
            return AppContext.BaseDirectory;
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static string GetLaunchCommand(string urlString = "<URL VARIABLE HERE>")
        {
            var binPath = GetBinaryPath().Trim();
            if (binPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || binPath.EndsWith(".so", StringComparison.OrdinalIgnoreCase))
            {
                binPath = $"\"{DotNetPath()}\" {binPath}";
            }
            else
            {
                binPath = $"{binPath}";
            }

            var cmd = $"{binPath} launch -u {urlString}";
            Serilog.Log.Information($"Launch cmd:{cmd}");
            return cmd;
        }
    }
}
