using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Sulu.Platform;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Sulu.Ui
{
    class Application : IApplication, IWebServer
    {
        Options Options { get; }
        Serilog.ILogger Logger { get; }
        int WebPort { get; }
        CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
        private IHost GenericHost { get; set; }
        private IUserInteraction UserInteraction { get; }
        private IOsServices OsServices { get; }

        public Application(Options options, Serilog.ILogger logger, IUserInteraction userInteraction, IOsServices osServices, ILifetimeScope container)
        {
            Options = options;
            Logger = logger;
            WebPort = GetRandomFreePort();
            OsServices = osServices;
            UserInteraction = userInteraction;

            // This is a hack, see the bottom of the file
            if (ExternalContainer != null)
            {
                throw new InvalidOperationException("External container was already initialized. Something is wrong with your autofac registrations.");
            }
            ExternalContainer = container;
        }

        public int Run()
        {
            UserInteraction.Message($"Sulu is starting up...");
            using (GenericHost = CreateHost())
            {
                var serverTask = GenericHost.RunAsync(CancellationTokenSource.Token).ContinueWith(x =>
                {
                    Log.Debug($"Web server stopped: {x.Status}");
                    return x;
                });
                OsServices.OpenDefault($"http://localhost:{WebPort}/index.html");
                UserInteraction.Message($"Sulu is running at http://localhost:{WebPort}. Close the browser window to quit.");
                GenericHost.WaitForShutdown();
            }
            return 0;
        }

        public void Shutdown()
        {
            GenericHost.StopAsync();
        }

        private IHost CreateHost()
        {
            var host = Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(x => ConfigureWebHostBuilder(x));
            return host.Build();
        }

        private IWebHostBuilder ConfigureWebHostBuilder(IWebHostBuilder builder)
        {
            return builder.UseStartup<Startup>()
                .UseKestrel(options => {
                    options.Listen(IPAddress.Loopback, WebPort); 
                })
                .UseContentRoot(Constants.GetBinaryDir())
                .UseSerilog(Logger, true);
        }

        

        private static int GetRandomFreePort()
        {
#if DEBUG
            return 42000;
#else
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return (listener.LocalEndpoint as IPEndPoint).Port;
            }
            finally
            {
                listener.Stop();
            }
#endif
        }

        // HAXX: This is a hack because we have two different autofac containers. 
        // One container is for the CLI, but when we switch to UI/webserver mode
        // we are using the Asp.Net Core container. Maybe there's a way to use the
        // existing container, but I haven't figured it out yet. So here' I'm just
        // injecting the one instance we need right now into the Asp.Net autofac 
        // container.
        private static ILifetimeScope ExternalContainer { get; set; } = null;

        internal static void RegisterExternalInstances(ContainerBuilder builder)
        {
            builder.RegisterInstance(ExternalContainer.ResolveNamed<IApplication>("ui")).As<IWebServer>().ExternallyOwned();
        }
    }
}
