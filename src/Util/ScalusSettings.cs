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
        private const string MinLogLevelSettings = "Logging:MinLevel";

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
                Console.WriteLine($"SHOUT - where is {fname}");
                throw new Exception($"Missing appsettings.json is {fname}");
            }
            Console.WriteLine($"SHOUT Reading configuration from path:{fname}");
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


        private static LogEventLevel ParseLevel()
        {
            var val = _appSetting[MinLogLevelSettings];
            object res;
            return Enum.TryParse(typeof(LogEventLevel), val, true, out res) ? Enum.Parse <LogEventLevel> (val) : LogEventLevel.Error;
        }
        public static LogEventLevel MinLogLevel => ParseLevel();
    }
}
