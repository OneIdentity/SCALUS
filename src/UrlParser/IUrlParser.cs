using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu.UrlParser
{
    interface IUrlParser : IDisposable
    {
        IDictionary<string, string> Parse(string url);
        bool WaitForProcessStartup { get; }
    }
}
