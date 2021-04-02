using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace scalus.UrlParser
{
    interface IUrlParser : IDisposable
    {
        IDictionary<string, string> Parse(string url);
        void PostExecute(Process process);
    }
}
