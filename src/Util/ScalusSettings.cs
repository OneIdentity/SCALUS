using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace scalus.Util
{
    public static class ConfigurationManager
    {
        private static IConfiguration _appSetting;
        private const string LogFileSetting = "Logging:fileName";
        private const string ConfigFileSetting = "Configuration:fileName";
        private const string MinLogLevelSetting = "Logging:MinLevel";
        private const string LogToConsoleSetting = "Logging:Console";
        private const string ProdName = "scalus";

        public static string ProdAppPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), ProdName) ;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), 
                    ProdName);
            }
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create), 
                $".{ProdName}");
        }
        private static string FullPath(string path)
        {
            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }

            var appDir = ProdAppPath();
            
            var fqpath = Path.Combine(appDir, path);
            var dir  = Path.GetDirectoryName(fqpath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return fqpath;
        }
        
        static ConfigurationManager()
        {
            var path = Constants.GetBinaryDirectory();
            var fname = Path.Combine(path, "appsettings.json");
            if (File.Exists(fname))
            {
                _appSetting = new ConfigurationBuilder()
                    .SetBasePath(path)
                    .AddJsonFile("appsettings.json", true)
                    .Build();
            }
        }

        public static string LogFile => string.IsNullOrEmpty(_appSetting?[LogFileSetting])
                    ? Path.Combine(Constants.GetBinaryDirectory(), "scalus.log")
                    : FullPath(_appSetting[LogFileSetting]);

        public static string ScalusJson => string.IsNullOrEmpty(_appSetting?[ConfigFileSetting])
                    ? Path.Combine(Constants.GetBinaryDirectory(), "scalus.json")
                    : FullPath(_appSetting[ConfigFileSetting]);

        public static string ScalusJsonDefault => Path.Combine(Constants.GetBinaryDirectory(), "scalus.json");

        private static LogEventLevel? ParseLevel()
        {
            var val = _appSetting?[MinLogLevelSetting]??string.Empty;
            if (string.IsNullOrEmpty(val))
                return null;
            object res;
            return Enum.TryParse(typeof(LogEventLevel), val, true, out res) ? Enum.Parse <LogEventLevel> (val) : LogEventLevel.Error;
        }
        public static LogEventLevel? MinLogLevel => ParseLevel();

        private static bool ParseConsoleLogging()
        {
            var val = _appSetting?[LogToConsoleSetting];
            bool  bval = false;
            if (bool.TryParse(val, out bval))
            {
                return bval;
            }
            return false;
        }
        public static bool LogToConsole => ParseConsoleLogging(); 
    }
}
