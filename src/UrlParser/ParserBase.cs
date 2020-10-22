using Sulu.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Sulu.UrlParser
{
    abstract class ParserBase : IUrlParser
    {
        protected ParserBase(ParserConfig config)
        {
            Config = config;
        }

        protected ParserConfig Config { get; }

        public abstract IDictionary<string, string> Parse(string url);

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

        public virtual void PostExecute(Process process)
        {
            if(Config.Options.Any(x => string.Equals(x, "waitforexit", StringComparison.OrdinalIgnoreCase)))
            {
                process.WaitForExit();
            }
            if (Config.Options.Any(x => string.Equals(x, "waitforinputidle", StringComparison.OrdinalIgnoreCase)))
            {
                process.WaitForInputIdle();
            }
            var wait = Config.Options.FirstOrDefault(x => x.StartsWith("wait:", StringComparison.OrdinalIgnoreCase));
            if(!string.IsNullOrEmpty(wait))
            {
                var parts = wait.Split(":");
                int time = 0;
                if (parts.Length > 1)
                {
                    int.TryParse(parts[1], out time);
                }
                if(time > 0)
                {
                    Task.Delay(time * 1000).Wait();
                }
            }
        }

        protected static string StripProtocol(string url)
        {
            var protocolIndex = url.IndexOf("://");
            if (protocolIndex == -1) return url;
            return url.Substring(protocolIndex + 3);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposables.Dispose();
                }

                disposedValue = true;
            }
        }

        private bool disposedValue;
    }
}
