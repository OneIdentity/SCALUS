using scalus.Dto;
using scalus.Platform;
using scalus.UrlParser;
using System;
using System.IO;

namespace scalus
{
    class ProtocolHandler : IProtocolHandler
    {
        private bool disposedValue;

        IUrlParser Parser { get; }
        string Uri { get; }
        IOsServices OsServices { get; }
        ApplicationConfig ApplicationConfig { get; }
        public ProtocolHandler(string uri, IUrlParser urlParser, ApplicationConfig applicationConfig, IOsServices osServices)
        {
            Uri = uri;
            Parser = urlParser;
            OsServices = osServices;
            ApplicationConfig = applicationConfig;
        }
        

        public void Run()
        {
            try {
                var dictionary = Parser.Parse(Uri);
                Parser.PreExecute(OsServices);           
                var args = Parser.ReplaceTokens(ApplicationConfig.Args);          
                Serilog.Log.Debug($"Starting external application: '{ApplicationConfig.Exec}' with args: '{string.Join(',', args)}'");
                if (!File.Exists(ApplicationConfig.Exec))
                {
                    Serilog.Log.Error($"Selected application does not exist:{ApplicationConfig.Exec}");
                    OsServices.OpenText($"Selected application does not exist:{ApplicationConfig.Exec}");

                    return;
                }
                var process = OsServices.Execute(ApplicationConfig.Exec, args);
                Serilog.Log.Debug("Post execute starting.");
                
                Parser.PostExecute(process);
                Serilog.Log.Debug("Post execute complete.");
            }
            catch (Exception e)
            {
                 OsServices.OpenText($"Launch failed: {e.Message}");

            }
        }

    
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Parser.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
