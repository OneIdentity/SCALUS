// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProtocolHandler.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.UrlParser;

    internal class ProtocolHandler : IProtocolHandler
    {
        private bool disposedValue;

        public ProtocolHandler(string uri, IUrlParser urlParser, ApplicationConfig applicationConfig, IOsServices osServices)
        {
            Uri = uri;
            Parser = urlParser;
            OsServices = osServices;
            ApplicationConfig = applicationConfig;
        }

        private IUrlParser Parser { get; }

        private string Uri { get; }

        private IOsServices OsServices { get; }

        private ApplicationConfig ApplicationConfig { get; }

        public static string PreviewOutput(IDictionary<ParserConfigDefinitions.Token, string> dictionary, string cmd, List<string> args)
        {
            var str = new StringBuilder();

            str.Append(@" 
 - Application:
   ----------
");
            str.Append(string.Format("   - {0,-16} : {1}{2}", "Application", cmd, Environment.NewLine));
            str.Append(string.Format("   - {0,-16} : {1}{2}", "Arguments", string.Join(',', args), Environment.NewLine));

            str.Append(@" 
 - Dictionary:
   ----------
");
            foreach (var (key, val) in dictionary)
            {
                str.Append(string.Format("   - {0,-16} : {1}{2}", key, val, Environment.NewLine));
            }

#pragma warning disable CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method
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
#pragma warning restore CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method

            return str.ToString();
        }

        public void Run(bool preview = false)
        {
            try
            {
                var dictionary = Parser.Parse(Uri);
                Parser.PreExecute(OsServices);
                var args = Parser.ReplaceTokens(ApplicationConfig.Args);

                var cmd = Parser.ReplaceTokens(ApplicationConfig.Exec.Trim());
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
                if (process == null)
                {
                    Serilog.Log.Error("Failed to create process for cmd:{cmd}");
                    throw new ProtocolException($"Failed to create process for cmd:{cmd}");
                }

                Serilog.Log.Debug("Post execute starting.");

                Parser.PostExecute(process);
                Serilog.Log.Debug("Post execute complete.");
            }
            catch (Exception e)
            {
                OsServices.OpenText($"Launch failed: {e.Message}");
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
    }
}
