using scalus.Dto;
using scalus.Platform;
using scalus.UrlParser;
using System;
using System.Collections.Generic;

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
            var variables = Parser.Parse(Uri);
            var args = ReplaceArgs(ApplicationConfig.Args, variables);

            Serilog.Log.Debug($"Starting external application: '{ApplicationConfig.Exec}' with args: '{string.Join(' ', args)}'");
            var process = OsServices.Execute(ApplicationConfig.Exec, args);
            Serilog.Log.Debug("Post execute starting.");
            Parser.PostExecute(process);
            Serilog.Log.Debug("Post execute complete.");
        }

        private IEnumerable<string> ReplaceArgs(IEnumerable<string> args, IDictionary<string,string> variables)
        {
            foreach (var arg in args)
            {
                var yarg = arg;
                foreach (var variable in variables)
                {
                    // TODO: Make this more robust. Edge case escapes don't work.
                    yarg = yarg.Replace($"${variable.Key}", variable.Value);
                }
                yield return yarg;
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
