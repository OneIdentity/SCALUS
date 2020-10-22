using System;
using System.Collections.Generic;

namespace Sulu.UrlParser
{
    interface IUrlParser : IDisposable
    {
        IDictionary<string, string> Parse(string url);
        bool WaitForProcessStartup { get; }
    }
}
