// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Ui
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using OneIdentity.Scalus.Platform;
    using Serilog;

    internal class Application : IApplication, IWebServer
    {
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

        private Options Options { get; }

        private Serilog.ILogger Logger { get; }

        private int WebPort { get; }

        private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        private IHost GenericHost { get; set; }

        private IUserInteraction UserInteraction { get; }

        private IOsServices OsServices { get; }

        private static ILifetimeScope ExternalContainer { get; set; }

        public int Run()
        {
            UserInteraction.Message($"SCALUS is starting up...");
            using (GenericHost = CreateHost())
            {
                var serverTask = GenericHost.RunAsync(CancellationTokenSource.Token).ContinueWith(x =>
                {
                    Log.Debug($"Web server stopped: {x.Status}");
                    return x;
                });
                OsServices.OpenDefault($"http://localhost:{WebPort}/index.html");
                UserInteraction.Message($"SCALUS is running at http://localhost:{WebPort}. Close the browser window to quit.");
                GenericHost.WaitForShutdown();
            }

            return 0;
        }

        public void Shutdown()
        {
            UserInteraction.Dispose();
            GenericHost.StopAsync();
        }

        // HAXX: This is a hack because we have two different autofac containers.
        // One container is for the CLI, but when we switch to UI/webserver mode
        // we are using the Asp.Net Core container. Maybe there's a way to use the
        // existing container, but I haven't figured it out yet. So here' I'm just
        // injecting the one instance we need right now into the Asp.Net autofac
        // container.
        internal static void RegisterExternalInstances(ContainerBuilder builder)
        {
            builder.RegisterInstance(ExternalContainer.ResolveNamed<IApplication>("ui")).As<IWebServer>().ExternallyOwned();
            builder.RegisterInstance(ExternalContainer.Resolve<IRegistration>()).ExternallyOwned();
            builder.RegisterInstance(ExternalContainer.ResolveNamed<IApplication>("info")).Named<IApplication>("InfoApplication").ExternallyOwned();
        }

        private IHost CreateHost()
        {
            var host = Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .UseSerilog(Logger, true)
                .ConfigureWebHostDefaults(x => ConfigureWebHostBuilder(x));
            return host.Build();
        }

        private IWebHostBuilder ConfigureWebHostBuilder(IWebHostBuilder builder)
        {
            return builder.UseStartup<Startup>()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, WebPort);
                })
                .UseContentRoot(Constants.GetBinaryDir());
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
    }
}
