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
        public const string ProdName = "scalus";
        private const string JsonFile = ProdName + ".json";
        private const string LogFileName = ProdName + ".log";
        private const string Examples = "examples";

        private static string _examplePath;
        public static string ExamplePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_examplePath))
                {
                    return _examplePath;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _examplePath = Path.Combine(Path.Combine(Path.GetDirectoryName(Constants.GetBinaryDirectory()), "Resources"), Examples);
                }
                else _examplePath = Path.Combine(Constants.GetBinaryDirectory(), Examples);
                if (Directory.Exists(_examplePath))
                {
                    return _examplePath;
                }
                _examplePath = string.Empty;
                return _examplePath;
            }
        }

        private static string _ProdAppPath;
        public static string ProdAppPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_ProdAppPath))
                    return _ProdAppPath;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _ProdAppPath= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), ProdName);
                    return _ProdAppPath;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var path =
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create), "Library");

                    _ProdAppPath = $"{path}/Application Support/{ProdName}";
                    if (!Directory.Exists(_ProdAppPath))
                    {
                        Directory.CreateDirectory(_ProdAppPath);
                    }
                    return _ProdAppPath;
                }
                _ProdAppPath= Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create),
                    $".{ProdName}");
                return _ProdAppPath;
            }
        }

        private static string FullPath(string path)
        {
            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }

            var appDir = ProdAppPath;
            
            var fqpath = Path.Combine(appDir, path);
            var dir  = Path.GetDirectoryName(fqpath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return fqpath;
        }
       
        private const string AppSettingDefault = "{\"Logging\": {\"FileName\": \"scalus.log\",\"MinLevel\": \"Information\",\"Console\": false},\"Configuration\": {\"FileName\":  \"scalus.json\"}}";
        static ConfigurationManager()
        {
            string path = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                path = ProdAppPath;
            }
            else path = Constants.GetBinaryDirectory();

            var fname = Path.Combine(path, "appsettings.json");
            
            if (File.Exists(fname))
            {
                _appSetting = new ConfigurationBuilder()
                    .SetBasePath(path)
                    .AddJsonFile("appsettings.json", true)
                    .Build();
            }
           
        }

        private static string _logFile;
        public static string LogFile
        {
            get
            {
                if (!string.IsNullOrEmpty(_logFile))
                    return _logFile;

                if (!string.IsNullOrEmpty(_appSetting?[LogFileSetting]))
                {
                    _logFile = FullPath(_appSetting[LogFileSetting]);
                    return _logFile;
                }
                _logFile = Path.Combine(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ProdAppPath : Constants.GetBinaryDirectory(), LogFileName);
                return _logFile;
            }
        }

        private static string _scalusJson;
        public static string ScalusJson
        {
            get
            {
                if (!string.IsNullOrEmpty(_scalusJson))
                    return _scalusJson;

                if (!string.IsNullOrEmpty(_appSetting?[ConfigFileSetting]))
                {
                    _scalusJson = FullPath(_appSetting[ConfigFileSetting]);
                    return _scalusJson;
                }

                var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ProdAppPath : Constants.GetBinaryDirectory();

                _scalusJson = Path.Combine(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ProdAppPath : Constants.GetBinaryDirectory(), JsonFile);
                return _scalusJson;
            }
        }

        private static string _scalusJsonDefault;
        public static string ScalusJsonDefault
        {
            get
            {
                if (!string.IsNullOrEmpty(_scalusJsonDefault))
                    return _scalusJsonDefault;
                _scalusJsonDefault = Path.Combine(Constants.GetBinaryDirectory(), JsonFile);
                if (!File.Exists(_scalusJsonDefault))
                {
                    _scalusJsonDefault = Path.Combine(Path.Combine(ExamplePath, JsonFile));
                }

                if (!File.Exists(_scalusJsonDefault))
                {
                    _scalusJsonDefault = string.Empty;
                }
                return _scalusJsonDefault;
            }
        }

        private static LogEventLevel? ParseLevel()
        {
            var val = _appSetting?[MinLogLevelSetting]??string.Empty;
            if (string.IsNullOrEmpty(val))
                return null;
            return Enum.TryParse(typeof(LogEventLevel), val, true, out _) ? Enum.Parse <LogEventLevel> (val) : LogEventLevel.Error;
        }
        public static LogEventLevel? MinLogLevel => ParseLevel();

        private static bool ParseConsoleLogging()
        {
            var val = _appSetting?[LogToConsoleSetting];
            if (bool.TryParse(val, out var bval))
            {
                return bval;
            }
            return false;
        }
        public static bool LogToConsole => ParseConsoleLogging(); 
    }
}
