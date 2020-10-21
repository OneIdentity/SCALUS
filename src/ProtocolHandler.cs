using Sulu.Dto;
using Sulu.Platform;
using Sulu.UrlParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu
{
    class ProtocolHandler : IProtocolHandler
    {
        private bool disposedValue;

        IUrlParser Parser { get; }
        string Uri { get; }
        IOsServices OsServices { get; }
        ProtocolConfig ProtocolConfig { get; }
        public ProtocolHandler(string uri, IUrlParser urlParser, ProtocolConfig protocolConfig, IOsServices osServices)
        {
            Uri = uri;
            Parser = urlParser;
            OsServices = osServices;
            ProtocolConfig = protocolConfig;
        }

        public void Run()
        {
            var variables = Parser.Parse(Uri);
            var args = ReplaceArgs(ProtocolConfig.Args, variables);

            Serilog.Log.Debug($"Starting external application: '{ProtocolConfig.Exec}' with args: '{string.Join(' ', args)}'");
            var process = OsServices.Execute(ProtocolConfig.Exec, args);
            if(Parser.WaitForProcessStartup)
            {
                process.WaitForInputIdle();
            }
        }

        private IEnumerable<string> ReplaceArgs(IEnumerable<string> args, IDictionary<string,string> variables)
        {
            foreach (var arg in args)
            {
                var yarg = arg;
                foreach (var variable in variables)
                {
                    // TODO: Make this more robust
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
