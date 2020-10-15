using Autofac;
using CommandLine;
using Serilog;
using System;
using System.Net.Http.Headers;
using System.Reflection;

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
                using var container = Ioc.RegisterApplication();
                using var lifetimeScope = container.BeginLifetimeScope();
                
                // Resolve the application builder to parse command line 
                // and resolve an appropriate application instance
                var builder = lifetimeScope.Resolve<IApplicationBuilder>();
                var application = builder.Build(args, x =>
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
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug()
                .CreateLogger();
        }
    }
}
