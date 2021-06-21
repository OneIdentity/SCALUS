using scalus.Dto;
using scalus.Platform;
using scalus.UrlParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Text;

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

        public string PreviewOutput(IDictionary<ParserConfigDefinitions.Token, string> dictionary, string cmd, List<string> args)
        {
            var str = new StringBuilder();
 
            str.Append(@" 
 - Application:
   ----------
");
            str.Append (string.Format(  "   - {0,-16} : {1}{2}", "Application", cmd, Environment.NewLine));
            str.Append (string.Format(  "   - {0,-16} : {1}{2}", "Arguments", string.Join(',', args), Environment.NewLine));

            str.Append(@" 
 - Dictionary:
   ----------
");
            foreach (var (key, val) in dictionary)
            {
                str.Append(string.Format("   - {0,-16} : {1}{2}", key, val, Environment.NewLine));
            }
            if (dictionary.ContainsKey(ParserConfigDefinitions.Token.GeneratedFile))
            {
                var fname = dictionary[ParserConfigDefinitions.Token.GeneratedFile];

                if (!string.IsNullOrEmpty(fname) && File.Exists(fname))
                {
                    var contents = File.ReadAllText(fname);
                    str.Append(string.Format("   - {0} : {1}", "Generated File Contents", Environment.NewLine));
                    str.Append(contents);
                }
            }

            return str.ToString();
        }
        public void Run(bool preview = false)
        {
            try {
                
                var dictionary = Parser.Parse(Uri);
                Parser.PreExecute(OsServices);           
                var args = Parser.ReplaceTokens(ApplicationConfig.Args);

                var cmd = Parser.ReplaceTokens(ApplicationConfig.Exec);
                Serilog.Log.Debug($"Starting external application: '{cmd}' with args: '{string.Join(',', args)}'");
                if (!File.Exists(cmd))
                {
                    Serilog.Log.Error($"Selected application does not exist:{cmd}");
                    OsServices.OpenText($"Selected application does not exist:{cmd}");

                    return;
                }

                if (preview)
                {
                    Serilog.Log.Information($"Preview mode - returning");
                    Console.WriteLine(PreviewOutput(dictionary, cmd, args));
                    return;
                }
                var process = OsServices.Execute(cmd, args);
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
