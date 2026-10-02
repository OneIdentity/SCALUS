// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BridgeDispatcher.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Ui
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Microsoft.Extensions.DependencyInjection;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;
    using Photino.NET;

    /// <summary>
    /// Bridges front-end requests to the in-process SCALUS core. Messages arrive as
    /// { id, method, args[] } and responses are returned as { id, ok, result|error }.
    /// </summary>
    internal sealed class BridgeDispatcher
    {
        private static readonly string[] BuiltInProtocols = { "rdp", "ssh" };

        // The front-end consumes PascalCase property names, so the response envelope keeps the
        // default (no naming policy) shape. Anonymous envelope types can't be source-generated,
        // so this reflection-based options instance is used only for the small wrapper object.
        private static readonly JsonSerializerOptions WriteOptions = new(JsonSerializerDefaults.General)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private readonly IServiceProvider services;
        private readonly IRegistration registration;
        private readonly IElevator elevator;
        private readonly string startupShowLogs;
        private PhotinoWindow window;

        public BridgeDispatcher(IServiceProvider services, string startupShowLogs = null)
        {
            this.services = services;
            this.startupShowLogs = startupShowLogs;
            this.registration = services.GetRequiredService<IRegistration>();
            this.elevator = services.GetRequiredService<IElevator>();
            SeedDefaultConfiguration();
        }

        public void Attach(PhotinoWindow host) => this.window = host;

        public string Dispatch(string message)
        {
            string id = null;
            try
            {
                var request = JsonNode.Parse(message).AsObject();
                id = request["id"]?.GetValue<string>();
                var method = request["method"]?.GetValue<string>();
                var args = request["args"] as JsonArray ?? new JsonArray();

                object result = method switch
                {
                    "getConfig" => GetConfig(),
                    "saveConfig" => SaveConfig(args[0].Deserialize<ScalusConfig>(ScalusJson.Disk)),
                    "validate" => Validate(args[0].Deserialize<ScalusConfig>(ScalusJson.Disk)),
                    "getRegistrations" => GetRegistrations(),
                    "getRegistrationStatus" => GetRegistrationStatus(args.Count > 0 ? args[0]?.GetValue<string>() : null),
                    "register" => Register(args[0]?.GetValue<string>(), args[1]?.GetValue<string>()),
                    "unregister" => Unregister(args[0]?.GetValue<string>(), args.Count > 1 ? args[1]?.GetValue<string>() : null),
                    "getTokens" => GetTokens(),
                    "getApplicationDescriptions" => GetApplicationDescriptions(),
                    "getParsers" => ProtocolHandlerFactory.GetSupportedParsers(),
                    "getTerminals" => GetTerminals(),
                    "getInfo" => GetInfo(),
                    "getVersion" => GetVersion(),
                    "getStartupAction" => GetStartupAction(),
                    "getLaunchRecords" => GetLaunchRecords(args.Count > 0 && args[0] != null ? args[0].GetValue<int>() : LaunchRecordStore.RetainedRecordLimit),
                    "getLaunchFile" => GetLaunchFile(args.Count > 0 ? args[0]?.GetValue<string>() : null),
                    "openLogsFolder" => OpenLogsFolder(),
                    "exportToFile" => ExportToFile(args[0]?.GetValue<string>(), args[1]?.GetValue<string>()),
                    "importFromFile" => ImportFromFile(),
                    "getPlatform" => GetPlatform(),
                    "getCapabilities" => GetCapabilities(),
                    _ => throw new InvalidOperationException($"Unknown method '{method}'."),
                };

                return JsonSerializer.Serialize(new { id, ok = true, result }, WriteOptions);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Bridge call failed");
                return JsonSerializer.Serialize(new { id, ok = false, error = ex.Message }, WriteOptions);
            }
        }

        private ScalusConfig GetConfig() =>
            this.services.GetRequiredService<IScalusApiConfiguration>().GetConfiguration();

        private object SaveConfig(ScalusConfig config)
        {
            var errors = this.services.GetRequiredService<IScalusApiConfiguration>().SaveConfiguration(config);
            return new { errors, config = errors.Count == 0 ? GetConfig() : config };
        }

        private List<string> Validate(ScalusConfig config)
        {
            var errors = new List<string>();
            config?.Validate(errors, false);
            return errors;
        }

        private List<string> GetRegistrations()
        {
            var config = GetConfig();
            var schemes = BuiltInProtocols
                .Concat(config.Protocols?.Select(p => p.Protocol) ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return schemes.Where(s => this.registration.IsRegistered(s)).ToList();
        }

        private List<RegistrationStatus> GetRegistrationStatus(string scope)
        {
            var rootMode = IsAllUsers(scope);
            var config = GetConfig();
            var schemes = BuiltInProtocols
                .Concat(config.Protocols?.Select(p => p.Protocol) ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            // Reads never elevate — detecting the machine layer needs no privileges — so the UI can
            // show either scope's status live. Only writes (Register/Unregister) elevate.
            return schemes.Select(s => this.registration.GetStatus(s, rootMode)).ToList();
        }

        private object Register(string protocol, string scope)
        {
            if (IsAllUsers(scope))
            {
                return RunElevated("register", protocol);
            }

            if (!this.registration.Register(new[] { protocol }, force: true, rootMode: false, useSudo: false))
            {
                throw new InvalidOperationException($"Failed to register '{protocol}'.");
            }

            return null;
        }

        private object Unregister(string protocol, string scope)
        {
            if (IsAllUsers(scope))
            {
                return RunElevated("unregister", protocol);
            }

            this.registration.UnRegister(new[] { protocol }, rootMode: false, useSudo: false);
            return null;
        }

        // All-users writes are delegated to an elevated, one-shot scalus process (UAC on Windows,
        // pkexec on Linux). The UI itself never runs elevated. A cancelled prompt is a benign no-op;
        // a genuine failure surfaces an error (with the manual sudo command on Linux when applicable).
        private object RunElevated(string verb, string protocol)
        {
            if (!this.elevator.CanElevate)
            {
                throw new InvalidOperationException("All-users registration is not supported on this platform.");
            }

            var result = this.elevator.Run(verb, new[] { protocol });
            if (result.Success || result.Cancelled)
            {
                return new { cancelled = result.Cancelled };
            }

            var message = result.Error ?? $"Failed to {verb} '{protocol}' for all users.";
            if (!string.IsNullOrEmpty(result.ManualCommand))
            {
                message += $" To do this manually, run: {result.ManualCommand}";
            }

            throw new InvalidOperationException(message);
        }

        private object GetCapabilities() => new
        {
            platform = GetPlatform(),
            canElevateAllUsers = this.elevator.CanElevate,
        };

        private static bool IsAllUsers(string scope) =>
            string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase);

        private Dictionary<string, string> GetTokens() =>
            ParserConfigDefinitions.TokenDescription.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);

        private Dictionary<string, string> GetApplicationDescriptions() =>
            ScalusConfig.DtoPropertyDescription;

        private List<object> GetTerminals() =>
            this.services.GetRequiredService<ITerminalResolver>()
                .GetAvailableTerminals()
                .Select(t => (object)new { t.Id, t.Name, t.Available })
                .ToList();

        private string GetInfo()
        {
            var config = GetConfig();
            var registered = GetRegistrations();
            var lines = new List<string>
            {
                $"Platform: {GetPlatform()}",
                $"Configuration file: {ConfigurationManager.ScalusJson}",
                $"Applications defined: {config.Applications?.Count ?? 0}",
                $"Protocols configured: {config.Protocols?.Count ?? 0}",
                $"Registered handlers: {(registered.Count == 0 ? "none" : string.Join(", ", registered))}",
            };
            return string.Join(Environment.NewLine, lines);
        }

        // The product version, sourced from the assembly's InformationalVersion (which flows
        // from Directory.Build.props - the single checked-in version source). SourceLink appends
        // "+<commit sha>" build metadata, which is trimmed so the UI shows a clean version.
        private static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informational))
            {
                var plus = informational.IndexOf('+');
                return plus >= 0 ? informational.Substring(0, plus) : informational;
            }

            return assembly.GetName().Version?.ToString() ?? "0.0.0";
        }

        // A one-time startup instruction for the front-end. Currently only carries the deep-link
        // launch id from a "--show-logs=<id>" invocation (spawned by the failure dialog).
        private object GetStartupAction() => new { ShowLogs = this.startupShowLogs };

        // Recent launch attempts (success and failure), newest first, for the Logs view.
        private List<LaunchRecord> GetLaunchRecords(int max) =>
            LaunchRecordStore.List(max).ToList();

        // Returns the contents of a file that lives beside a launch record (e.g. the generated
        // .rdp/ssh config). Only the file name is honored so a request can't escape the launches dir.
        private string GetLaunchFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var path = Path.Combine(ConfigurationManager.LaunchRecordsDir, Path.GetFileName(fileName));
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        // Opens the per-user logs folder in the OS file manager so the user can grab the files.
        private bool OpenLogsFolder()
        {
            this.services.GetRequiredService<IOsServices>().OpenDefault(ConfigurationManager.LogDir);
            return true;
        }

        private bool ExportToFile(string defaultName, string contents)
        {
            if (this.window == null)
            {
                return false;
            }

            // Photino's native save dialog skips display entirely when defaultPath does not
            // resolve to an existing item, so open in an existing folder rather than passing a
            // suggested (non-existent) file name.
            var target = this.window.ShowSaveFile(
                title: "Export configuration",
                defaultPath: Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                filters: new (string, string[])[] { ("JSON", new[] { "json" }), ("All files", new[] { "*" }) });

            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            // The native dialog does not enforce a default extension, so add one when the
            // chosen name has none.
            if (string.IsNullOrEmpty(Path.GetExtension(target)))
            {
                target = Path.ChangeExtension(target, "json");
            }

            File.WriteAllText(target, contents);
            return true;
        }

        private string ImportFromFile()
        {
            if (this.window == null)
            {
                return null;
            }

            var selected = this.window.ShowOpenFile(
                title: "Import configuration",
                multiSelect: false,
                filters: new (string, string[])[] { ("JSON", new[] { "json" }), ("All files", new[] { "*" }) });

            var path = selected?.FirstOrDefault();
            return string.IsNullOrEmpty(path) ? null : File.ReadAllText(path);
        }

        private static string GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "Mac";
            }

            return "Linux";
        }

        private void SeedDefaultConfiguration()
        {
            try
            {
                var api = this.services.GetRequiredService<IScalusApiConfiguration>();

                // Repair legacy/empty templates on disk so CLI launches (which read the same file)
                // get working generated files even if the user never re-saves from the UI.
                api.MigrateOnDisk();

                var config = api.GetConfiguration();
                var changed = false;

                // First run (or a previously emptied config): preload the shipped
                // applications so the Applications screen isn't blank, and present the
                // built-in protocols as unconfigured so nothing is registered without
                // the user explicitly choosing an application.
                if (config.Applications == null || config.Applications.Count == 0)
                {
                    var seedApps = LoadSeedApplications();
                    if (seedApps.Count > 0)
                    {
                        config.Applications = seedApps;
                        config.Protocols = BuiltInProtocols
                            .Select(p => new ProtocolMapping { Protocol = p, AppId = string.Empty })
                            .ToList();
                        changed = true;
                        Serilog.Log.Information(
                            "Seeded {Count} default applications with unconfigured built-in protocols",
                            seedApps.Count);
                    }
                }
                else if (ScalusConfigurationBase.RepairMissingTemplatesFromSeed(config, LoadSeedApplications()))
                {
                    // An existing config can have lost the inline template for an application
                    // (older or hand-edited configs). Restore it from the shipped defaults by Id
                    // so the generated file isn't empty and launches keep working.
                    changed = true;
                }

                // The built-in protocols can never be deleted, so make sure a row exists
                // for each one even in a hand-edited or imported configuration.
                if (EnsureBuiltInProtocols(config))
                {
                    changed = true;
                }

                if (changed)
                {
                    api.SaveConfiguration(config);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to seed the default configuration");
            }
        }

        private static bool EnsureBuiltInProtocols(ScalusConfig config)
        {
            config.Protocols ??= new List<ProtocolMapping>();
            var changed = false;
            foreach (var scheme in BuiltInProtocols)
            {
                if (!config.Protocols.Any(p =>
                    string.Equals(p.Protocol, scheme, StringComparison.OrdinalIgnoreCase)))
                {
                    config.Protocols.Add(new ProtocolMapping { Protocol = scheme, AppId = string.Empty });
                    changed = true;
                }
            }

            return changed;
        }

        private static List<ApplicationConfig> LoadSeedApplications()
        {
            // One shipped master seed (defaults/SCALUS.json) lists every application across all
            // platforms; filter it down to the OS we're running on so a fresh config only offers
            // applications that are valid here.
            var seed = Path.Combine(AppContext.BaseDirectory, "defaults", "SCALUS.json");
            if (!File.Exists(seed))
            {
                return new List<ApplicationConfig>();
            }

            try
            {
                var parsed = ScalusJson.Deserialize(File.ReadAllText(seed));
                return PlatformFilter.ForCurrentPlatform(parsed?.Applications);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to read seed applications from {Seed}", seed);
                return new List<ApplicationConfig>();
            }
        }
    }
}
