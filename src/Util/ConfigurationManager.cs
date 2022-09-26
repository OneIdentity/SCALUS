// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationManager.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Util
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Configuration;
    using Serilog.Events;

    public static class ConfigurationManager
    {
        public const string ProdName = "scalus";

        private const string LogFileSetting = "Logging:fileName";
        private const string ConfigFileSetting = "Configuration:fileName";
        private const string MinLogLevelSetting = "Logging:MinLevel";
        private const string LogToConsoleSetting = "Logging:Console";
        private const string JsonFile = ProdName + ".json";
        private const string LogFileName = ProdName + ".log";
        private const string Examples = "examples";

        private static string examplePath;
        private static string prodAppPath;
        private static string logFile;
        private static string scalusJson;
        private static string scalusJsonDefault;

        private static IConfiguration appSetting;

        static ConfigurationManager()
        {
            string path = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                path = ProdAppPath;
            }
            else
            {
                path = Constants.GetBinaryDirectory();
            }

            var fname = Path.Combine(path, "appsettings.json");

            if (File.Exists(fname))
            {
                appSetting = new ConfigurationBuilder()
                    .SetBasePath(path)
                    .AddJsonFile("appsettings.json", true)
                    .Build();
            }
        }

        public static string ExamplePath
        {
            get
            {
                if (!string.IsNullOrEmpty(examplePath))
                {
                    return examplePath;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    examplePath = Path.Combine(Path.Combine(Path.GetDirectoryName(Constants.GetBinaryDirectory()), "Resources"), Examples);
                }
                else
                {
                    examplePath = Path.Combine(Constants.GetBinaryDirectory(), Examples);
                }

                if (Directory.Exists(examplePath))
                {
                    return examplePath;
                }

                examplePath = string.Empty;
                return examplePath;
            }
        }

        public static string ProdAppPath
        {
            get
            {
                if (!string.IsNullOrEmpty(prodAppPath))
                {
                    return prodAppPath;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    prodAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), ProdName);
                    if (!Directory.Exists(prodAppPath))
                    {
                        Directory.CreateDirectory(prodAppPath);
                    }

                    return prodAppPath;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var path =
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create), "Library");

                    prodAppPath = $"{path}/Application Support/{ProdName}";
                    if (!Directory.Exists(prodAppPath))
                    {
                        Directory.CreateDirectory(prodAppPath);
                    }

                    return prodAppPath;
                }

                prodAppPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create),
                    $".{ProdName}");
                if (!Directory.Exists(prodAppPath))
                {
                    Directory.CreateDirectory(prodAppPath);
                }

                return prodAppPath;
            }
        }

        public static string LogFile
        {
            get
            {
                if (!string.IsNullOrEmpty(logFile))
                {
                    return logFile;
                }

                if (!string.IsNullOrEmpty(appSetting?[LogFileSetting]))
                {
                    logFile = FullPath(appSetting[LogFileSetting]);
                    return logFile;
                }

                logFile = Path.Combine(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ProdAppPath : Constants.GetBinaryDirectory(), LogFileName);
                return logFile;
            }
        }

        public static string ScalusJson
        {
            get
            {
                if (!string.IsNullOrEmpty(scalusJson))
                {
                    return scalusJson;
                }

                if (!string.IsNullOrEmpty(appSetting?[ConfigFileSetting]))
                {
                    scalusJson = FullPath(appSetting[ConfigFileSetting]);
                    return scalusJson;
                }

                var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ProdAppPath : Constants.GetBinaryDirectory();

                scalusJson = Path.Combine(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ProdAppPath : Constants.GetBinaryDirectory(), JsonFile);
                return scalusJson;
            }
        }

        public static string ScalusJsonDefault
        {
            get
            {
                if (!string.IsNullOrEmpty(scalusJsonDefault))
                {
                    return scalusJsonDefault;
                }

                scalusJsonDefault = Path.Combine(Constants.GetBinaryDirectory(), JsonFile);
                if (!File.Exists(scalusJsonDefault))
                {
                    scalusJsonDefault = Path.Combine(Path.Combine(ExamplePath, JsonFile));
                }

                if (!File.Exists(scalusJsonDefault))
                {
                    scalusJsonDefault = string.Empty;
                }

                return scalusJsonDefault;
            }
        }

        public static LogEventLevel? MinLogLevel => ParseLevel();

        public static bool LogToConsole => ParseConsoleLogging();

        private static string FullPath(string path)
        {
            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }

            var appDir = ProdAppPath;

            var fqpath = Path.Combine(appDir, path);
            var dir = Path.GetDirectoryName(fqpath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return fqpath;
        }

        private static LogEventLevel? ParseLevel()
        {
            var val = appSetting?[MinLogLevelSetting] ?? string.Empty;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return Enum.TryParse(typeof(LogEventLevel), val, true, out _) ? Enum.Parse<LogEventLevel>(val) : LogEventLevel.Error;
        }

        private static bool ParseConsoleLogging()
        {
            var val = appSetting?[LogToConsoleSetting];
            if (bool.TryParse(val, out var bval))
            {
                return bval;
            }

            return false;
        }
    }
}
