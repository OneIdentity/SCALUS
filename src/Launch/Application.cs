using Sulu.Platform;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Policy;

namespace Sulu.Launch
{
    class Application : IApplication
    {
        Launch.Options Options { get; }

        ISuluConfiguration Config { get; }

        IOsServices OsServices { get; }

        public Application(Launch.Options options, IOsServices osServices, ISuluConfiguration config)
        {
            this.Options = options;
            Config = config;
            OsServices = osServices;
        }

        public int Run()
        {
            Serilog.Log.Debug($"Dispatching URL: {Options.Url}");

            try
            {
                using var handler = Config.GetProtocolHandler(Options.Url);
                if(handler == null)
                {
                    // We are the registered application, but we can't parse the config
                    // or nothing is configured, show an error somehow
                    LaunchTextFile($"Sulu configuration does not provide a method to handle the URL: {Options.Url}");
                    return 1;
                }
                handler.Run();
                return 0;
            }
            catch(Exception ex)
            {
                Serilog.Log.Error($"Failed launching URL: {ex.Message}", ex);
            }
            return 1;
        }

        private void LaunchTextFile(string text)
        {
            var tempFile = Path.GetTempFileName() + ".txt";
            try
            {
                File.WriteAllText(tempFile, text);
                OsServices.OpenDefault(tempFile).WaitForExit();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"Failed launching URL: {ex.Message}", ex);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
