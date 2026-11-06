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

        public ProtocolHandler(string uri, IUrlParser urlParser, ApplicationConfig applicationConfig, IOsServices osServices, ITerminalResolver terminalResolver = null, string preferredTerminal = null)
        {
            Uri = uri;
            Parser = urlParser;
            OsServices = osServices;
            ApplicationConfig = applicationConfig;
            TerminalResolver = terminalResolver;
            PreferredTerminal = preferredTerminal;
        }

        private IUrlParser Parser { get; }

        private string Uri { get; }

        private IOsServices OsServices { get; }

        private ApplicationConfig ApplicationConfig { get; }

        private ITerminalResolver TerminalResolver { get; }

        private string PreferredTerminal { get; }

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

            if (dictionary.TryGetValue(ParserConfigDefinitions.Token.GeneratedFile, out var fname))
            {
                if (!string.IsNullOrEmpty(fname) && File.Exists(fname))
                {
                    var contents = File.ReadAllText(fname);
                    str.Append(string.Format("   - {0} : {1}", "Generated File Contents", Environment.NewLine));
                    str.Append(contents);
                }
            }

            return str.ToString();
        }

        public LaunchResult Run(bool preview = false, string generatedFileDirectory = null, string generatedFileBaseName = null)
        {
            var result = new LaunchResult { ApplicationId = ApplicationConfig?.Id };
            var spawned = false;
            try
            {
                // For a real launch, tell the parser to persist its generated file (e.g. the .rdp file)
                // into the launches directory alongside the record instead of a self-deleting temp file.
                if (!preview && !string.IsNullOrEmpty(generatedFileDirectory))
                {
                    Parser.SetGeneratedFileTarget(generatedFileDirectory, generatedFileBaseName);
                }

                var dictionary = Parser.Parse(Uri);
                Parser.PreExecute(OsServices);
                var args = Parser.ReplaceTokens(ApplicationConfig.Args);

                var cmd = Parser.ReplaceTokens(ApplicationConfig.Exec.Trim());
                result.Command = cmd;
                result.Args = string.Join(' ', args);
                CaptureGeneratedFile(dictionary, result);
                Serilog.Log.Debug($"Starting external application: '{cmd}' with args: '{result.Args}'");
                if (!File.Exists(cmd))
                {
                    var msg = $"Selected application does not exist: {cmd}";
                    Serilog.Log.Error(msg);

                    result.Outcome = LaunchOutcome.ConfigError;
                    result.Error = msg;
                    return result;
                }

                // When the application is a terminal-based client (e.g. SSH), host it in the user's
                // preferred terminal. The existence check above still validates the inner program;
                // wrapping only changes which process is actually spawned.
                var execCmd = cmd;
                var execArgs = args;
                if (ApplicationConfig.Parser?.RunInTerminal == true && TerminalResolver != null)
                {
                    var wrapped = TerminalResolver.Wrap(cmd, args, PreferredTerminal);
                    execCmd = wrapped.Exec;
                    execArgs = wrapped.Args as List<string> ?? new List<string>(wrapped.Args);
                    result.Command = execCmd;
                    result.Args = string.Join(' ', execArgs);
                    Serilog.Log.Debug($"Hosting terminal launch in: '{execCmd}' with args: '{result.Args}'");
                }

                if (preview)
                {
                    Serilog.Log.Information($"Preview mode - returning");
                    Console.WriteLine(PreviewOutput(dictionary, execCmd, execArgs));
                    result.Outcome = LaunchOutcome.Preview;
                    return result;
                }

                var process = OsServices.Execute(execCmd, execArgs);
                if (process == null)
                {
                    var msg = $"Failed to create process for cmd: {execCmd}";
                    Serilog.Log.Error(msg);
                    throw new ProtocolException(msg);
                }

                spawned = true;
                Serilog.Log.Debug("Post execute starting.");

                Parser.PostExecute(process);
                Serilog.Log.Debug("Post execute complete.");

                result.Outcome = LaunchOutcome.Spawned;
                result.ExitCode = process.HasExited ? process.ExitCode : null;
                return result;
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, $"Launch failed: {e.Message}");
                result.Outcome = spawned ? LaunchOutcome.PostExecuteError : LaunchOutcome.SpawnFailed;
                result.Error = e.Message;
                return result;
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

        // Records the path of the parser-materialized file (e.g. the .rdp file or ssh config) onto the
        // launch result. For a real launch this is the persisted file inside the launches directory;
        // this is where launch problems usually hide, so it is captured even when a launch later fails.
        private static void CaptureGeneratedFile(IDictionary<ParserConfigDefinitions.Token, string> dictionary, LaunchResult result)
        {
            if (dictionary.TryGetValue(ParserConfigDefinitions.Token.GeneratedFile, out var fname)
                && !string.IsNullOrEmpty(fname))
            {
                result.GeneratedFile = fname;
            }
        }
    }
}
