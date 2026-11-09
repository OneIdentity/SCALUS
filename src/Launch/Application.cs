// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Launch
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;
    using Serilog.Context;

    internal class Application : IApplication
    {
        public Application(Launch.Options options, IOsServices osServices, IScalusConfiguration config)
        {
            this.Options = options;
            Config = config;
            OsServices = osServices;
        }

        private Launch.Options Options { get; }

        private IScalusConfiguration Config { get; }

        private IOsServices OsServices { get; }

        public int Run()
        {
            // A short correlation id shared by every log line and the persisted record for this launch.
            var launchId = Guid.NewGuid().ToString("N").Substring(0, 12);
            var timestamp = DateTime.UtcNow;

            // One base name shared by the record ({base}.json) and the parser's generated file
            // ({base}{ext}) so they sit together in the launches directory.
            var baseName = $"{timestamp:yyyyMMddTHHmmssfffZ}-{launchId}";
            var record = new LaunchRecord
            {
                LaunchId = launchId,
                TimestampUtc = timestamp,
                Protocol = GetProtocol(Options.Url),
                Url = Options.Url,
                LauncherBinary = CurrentBinaryName(),
            };
            var stopwatch = Stopwatch.StartNew();

            // The trailing space keeps the log template tidy ("[abc] message") and renders as nothing
            // when the property is absent on non-launch log lines.
            using (LogContext.PushProperty("LaunchId", $"[{launchId}] "))
            {
                Serilog.Log.Debug($"Dispatching URL: {Options.Url}");

                try
                {
                    using var handler = Config.GetProtocolHandler(Options.Url);
                    if (handler == null)
                    {
                        // We are the registered application, but we can't parse the config
                        // or nothing is configured, show an error somehow
                        var msg = $"SCALUS configuration does not provide a method to handle the URL: {Options.Url}";
                        record.Outcome = "config-error";
                        record.Error = msg;
                        return Finish(record, stopwatch, 1, baseName);
                    }

                    var launchesDir = Options.Preview ? null : ConfigurationManager.LaunchRecordsDir;
                    var result = handler.Run(Options.Preview, launchesDir, baseName);
                    record.ApplicationId = result.ApplicationId;
                    record.Command = result.Command;
                    record.Args = result.Args;
                    record.Outcome = LaunchResult.OutcomeKeyword(result.Outcome);
                    record.ExitCode = result.ExitCode;
                    record.Error = result.Error;

                    // For a real launch the parser wrote its file into the launches directory next to
                    // the record; store just the file name so the UI can find it beside the .json.
                    if (!Options.Preview && !string.IsNullOrEmpty(result.GeneratedFile))
                    {
                        record.GeneratedFile = Path.GetFileName(result.GeneratedFile);
                    }

                    return Finish(record, stopwatch, result.Success ? 0 : 1, baseName);
                }
                catch (Exception ex)
                {
                    HandleLaunchError(ex, Options.Url);
                    record.Outcome = "error";
                    record.Error = ex.Message;
                    return Finish(record, stopwatch, 1, baseName);
                }
            }
        }

        internal static string GetProtocol(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            // Scheme is everything before the first ':'. Handles both hierarchical URLs
            // (ssh://host) and the opaque form Safeguard issues for RDP (rdp:full+address=...).
            var colonIndex = url.IndexOf(':');
            if (colonIndex <= 0)
            {
                return string.Empty;
            }

            return url.Substring(0, colonIndex);
        }

        private static string CurrentBinaryName()
        {
            try
            {
                return Path.GetFileNameWithoutExtension(Environment.ProcessPath);
            }
            catch
            {
                return null;
            }
        }

        private static ApplicationConfig GetApplicationForProtocol(ScalusConfig config, string protocol)
        {
            var application = config.Protocols.FirstOrDefault(x => x.Protocol == protocol);
            if (application == null)
            {
                return null;
            }

            return config.Applications.FirstOrDefault(x => x.Id == application.AppId);
        }

        private int Finish(LaunchRecord record, Stopwatch stopwatch, int exitCode, string baseName)
        {
            stopwatch.Stop();
            record.DurationMs = stopwatch.ElapsedMilliseconds;
            record.Success = exitCode == 0;
            LaunchRecordStore.Write(record, baseName);

            // Every failed real launch (never a preview) surfaces a native dialog that deep-links
            // the Logs view to this record. The record is already persisted above so the UI can
            // find it. This replaces the old temp-.txt-in-Notepad failure path.
            if (exitCode != 0 && !Options.Preview)
            {
                OsServices.ShowLaunchFailure(BuildFailureMessage(record), record.LaunchId);
            }

            return exitCode;
        }

        private static string BuildFailureMessage(LaunchRecord record)
        {
            var reason = string.IsNullOrWhiteSpace(record.Error) ? record.Outcome : record.Error;
            var what = string.IsNullOrEmpty(record.Protocol) ? "the requested" : record.Protocol;
            return $"SCALUS could not launch {what} session.\n\n{reason}";
        }

        private void HandleLaunchError(Exception ex, string url)
        {
            ApplicationConfig application = null;
            var scalusJsonPath = ConfigurationManager.ScalusJson;

            try
            {
                application = GetApplicationForProtocol(Config.GetConfiguration(), GetProtocol(url));
            }
            catch (Exception e)
            {
                OsServices.ShowMessage($"Failed to read config file: {scalusJsonPath}: {e.Message}");
            }

            var msg =
$@"[SCALUS]: Failed to launch registered URL handler.
  URL:           {url}
  Error:         {ex.Message}

  ApplicationId: {application?.Id ?? "<none>"}
  Command:       {application?.Exec ?? "<none>"}
  Args:          {(application?.Args != null ? string.Join(" ", application?.Args) : "<none>")}

  Config File:   {scalusJsonPath}  

Check the configuration for this URL protocol.";
            Serilog.Log.Error(ex, msg);
        }
    }
}
