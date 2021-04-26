using System;
using System.IO;
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

        private static string FullPath(string path)
        {
            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }
            
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path);
        }
        
        static ConfigurationManager()
        {
            var path = Constants.GetBinaryDirectory();
            var fname = Path.Combine(path, "appsettings.json");
            if (!File.Exists(fname))
            {
                throw new Exception($"Missing appsettings.json is {fname}");
            }
            _appSetting = new ConfigurationBuilder()
                
                .SetBasePath(path)
                .AddJsonFile("appsettings.json", true)
                .Build();
        }

        public static string LogFile => string.IsNullOrEmpty(_appSetting[LogFileSetting])
                    ? Path.Combine(Constants.GetBinaryDirectory(), "scalus.log")
                    : FullPath(_appSetting[LogFileSetting]);

        public static string ScalusJson => string.IsNullOrEmpty(_appSetting[ConfigFileSetting])
                    ? Path.Combine(Constants.GetBinaryDirectory(), "scalus.json")
                    : FullPath(_appSetting[ConfigFileSetting]);


        private static LogEventLevel? ParseLevel()
        {
            var val = _appSetting[MinLogLevelSetting];
            if (string.IsNullOrEmpty(val))
                return null;
            object res;
            return Enum.TryParse(typeof(LogEventLevel), val, true, out res) ? Enum.Parse <LogEventLevel> (val) : LogEventLevel.Error;
        }
        public static LogEventLevel? MinLogLevel => ParseLevel();

        private static bool ParseConsoleLogging()
        {
            var val = _appSetting[LogToConsoleSetting];
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
