using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sulu.Ui
{
    class Application : IApplication
    {
        Options Options { get; }

        Serilog.ILogger Logger { get; }

        int WebPort { get; }

        public Application(Options options, Serilog.ILogger logger)
        {
            Options = options;
            Logger = logger;
            WebPort = 42420;
        }

        public int Run()
        {
            Serilog.Log.Debug("Ui!");

            using var host = CreateHost();
            host.Start();

            Serilog.Log.Debug($"Web server stopped.");

            // var browserProcess = OpenBrowser($"http://localhost:{WebPort}/index.html");
            // wait for browser to close
            
            return 0;
        }

        private IHost CreateHost()
        {
            var host = Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(x => ConfigureWebHostBuilder(x));
            return host.Build();
        }

        private IWebHost CreateWebHost()
        {
            var builder = ConfigureWebHostBuilder(WebHost.CreateDefaultBuilder());
            return builder.Build();
        }

        private IWebHostBuilder ConfigureWebHostBuilder(IWebHostBuilder builder)
        {
            return builder.UseStartup<Startup>()
                .UseUrls("http://localhost:42420")
                .UseContentRoot(Constants.GetBinaryDir())
                .UseSerilog(Logger, true);
        }

        public static Process OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); // Works ok on windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Process.Start("xdg-open", url);  // Works ok on linux
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Process.Start("open", url); // Not tested
            }
            return null;
        }
    }
}
