using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Sulu.UrlParser
{
    abstract class ParserBase : IUrlParser
    {
        public abstract IDictionary<string, string> Parse(string url);

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

        public virtual bool WaitForProcessStartup => false;

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
