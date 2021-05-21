using Autofac;
using CommandLine;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using scalus.Util;
using Serilog.Core;
using Serilog.Events;

[assembly:InternalsVisibleTo("scalus.Test")]

namespace scalus
{
    class Program
    {
        static int Main(string[] args)
        {
            ConfigureLogging();
            CheckConfig();
            try
            {
                // Register components with autofac
                using var container = Ioc.RegisterApplication(Log.Logger);
                using var lifetimeScope = container.BeginLifetimeScope();
                
                // Resolve the command line parser and 
                // resolve a corresponding application instance
                var parser = lifetimeScope.Resolve<ICommandLineParser>();
                var application = parser.Build(args, x =>
                {
                    var type = x.GetType();
                    var verb = type.GetCustomAttribute<VerbAttribute>().Name;
                    return lifetimeScope.ResolveNamed<IApplication>(verb, new TypedParameter(type, x));
                });

                // If application is null, then they ran help or version commands, just return
                if (application == null) return 0;

                // Run application
                return application.Run();
            }
            catch(CommandLineHelpException ex)
            {
                // Command line usage
                Console.WriteLine(ex.Message);
            }
            catch(Exception ex)
            {
                HandleUnexpectedError(ex);
            }
            return 1;
        }

        private static void HandleUnexpectedError(Exception ex)
        {
            Serilog.Log.Error($"Unexpected error: {ex.Message}", ex);
            string indent = "  ";
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                Serilog.Log.Error($"{indent}=> Inner Exception: {ex.Message}", ex);
                indent += "  ";
            }
        }


        static void CheckConfig()
        {
            Log.Logger.Information($"CheckConfig");
            if (File.Exists(ConfigurationManager.ScalusJson))
            {
                Log.Logger.Information($"ok");
                return;
            }

            if (!File.Exists(ConfigurationManager.ScalusJsonDefault)) 
            { 
                Log.Logger.Warning($"Config file not found:{ConfigurationManager.ScalusJson} and installed file not found:{ConfigurationManager.ScalusJsonDefault}");
                return;
            }
            
            try
            {
                var dir = Path.GetDirectoryName(ConfigurationManager.ScalusJson);
                if (!Directory.Exists(dir)) 
                { 
                    Directory.CreateDirectory(dir);
                }
                Log.Logger.Information($"Initializing config file:{ConfigurationManager.ScalusJsonDefault} from the installed file:{ConfigurationManager.ScalusJsonDefault}");
                File.WriteAllText(ConfigurationManager.ScalusJson,  File.ReadAllText(ConfigurationManager.ScalusJsonDefault));
            }
            catch (Exception e) {
                Log.Logger.Information($"Failed to initialize config file:{ConfigurationManager.ScalusJson} from installed file:{ConfigurationManager.ScalusJsonDefault}: {e.Message}");
            }
        }
        static void ConfigureLogging()
        {
            var logFilePath = ConfigurationManager.LogFile;
            var config = new LoggerConfiguration();
            config.WriteTo.File(logFilePath, shared: true);
            if (ConfigurationManager.MinLogLevel != null)
            {
                config.MinimumLevel.ControlledBy(new LoggingLevelSwitch(ConfigurationManager.MinLogLevel.Value));
            }
            if (ConfigurationManager.LogToConsole)
            {
                config.WriteTo.Console();
            }
            Log.Logger = config.CreateLogger();
        }
    }
}
