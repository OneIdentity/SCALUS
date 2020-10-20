using Autofac;
using CommandLine;
using Serilog;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Sulu.Test, PublicKey=00240000048000009400000006020000002400"+  
    "0052534131000400000100010099f5235dfcb30256799efe82c00b6aa085fbad16977043cab35fc43317"+
    "44d27ba5d2347006da0fb8c23e92ce81a934dad77a1d2cdb7946c9b4eb956327d5d6e71c21a2dec18529"+
    "376f9e828072f9780feaf3f006c02aabef20bdd04d9adcbca40e00abea656d5dbbc1bb9048dc5ab262e9"+
    "41e30c5ac4b97591d0d507489337ce")]

namespace Sulu
{
    class Program
    {
        static int Main(string[] args)
        {
            ConfigureLogging();

            try
            {
                // Register components with autofac
                using var container = Ioc.RegisterApplication(Serilog.Log.Logger);
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

        

        static void ConfigureLogging()
        {
            var folder = Constants.GetBinaryDir();
            var logFilePath = System.IO.Path.Combine(folder, "sulu.log");

            var config = new LoggerConfiguration();
            config.WriteTo.File(logFilePath, shared: true)
                .MinimumLevel.Debug();
#if DEBUG
            config.WriteTo.Console();
#endif
            Log.Logger = config.CreateLogger();
        }
    }
}
