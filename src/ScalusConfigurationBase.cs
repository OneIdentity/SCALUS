// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScalusConfigurationBase.cs" company="One Identity Inc.">
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
    using System.Text.Json;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Util;

    internal class ScalusConfigurationBase
    {
        private ScalusConfig scalusConfig;

        protected ScalusConfigurationBase()
        {
        }

        public List<string> ValidationErrors { get; protected set; } = new List<string>();

        protected string configFile { get; } = ConfigurationManager.ScalusJson;

        protected ScalusConfig Config
        {
            get
            {
                if (scalusConfig == null)
                {
                    scalusConfig = Load(configFile);
                }

                return scalusConfig;
            }

            set
            {
                scalusConfig = value;
            }
        }

        public ScalusConfig GetConfiguration()
        {
            // Return a shallow copy that preserves ALL top-level fields. Historically this stripped
            // everything except Applications/Protocols, which silently dropped PreferredTerminal (and
            // now Settings) on read — and because the UI then saves back the object it read, the next
            // save wiped the persisted values. Copy every field so the UI round-trips faithfully.
            return new ScalusConfig
            {
                Applications = Config.Applications,
                Protocols = Config.Protocols,
                PreferredTerminal = Config.PreferredTerminal,
                Settings = Config.Settings,
            };
        }

        public (bool, ScalusConfig) Validate(string json, bool strict = false)
        {
            var config = new ScalusConfig();
            ValidationErrors = new List<string>();
            try
            {
                config = ScalusJson.Deserialize(json, strict);
            }
            catch (Exception e)
            {
                ValidationErrors.Add($"Error deserialising json configuration: {e.Message}");
            }

            try
            {
                config?.Validate(ValidationErrors, false);
            }
            catch (Exception e)
            {
                ValidationErrors.Add($"Error validating json configuration: {e.Message}");
                return (false, config);
            }

            return (ValidationErrors.Count == 0, config);
        }

        // One-time upgrade: fold legacy template mechanisms (UseDefaultTemplate / UseTemplateFile) into
        // inline TemplateContent so the template is visible and travels with the configuration. Runs in
        // memory on every load; once the config is re-saved the legacy fields disappear. Returns true if
        // anything was changed so callers can choose to persist.
        internal static bool MigrateLegacyTemplates(ScalusConfig config)
        {
            if (config?.Applications == null)
            {
                return false;
            }

            var changed = false;
            foreach (var app in config.Applications)
            {
                var parser = app?.Parser;
                if (parser == null || parser.HasTemplate)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(parser.UseTemplateFile))
                {
                    var resolved = ResolveConfigTokens(parser.UseTemplateFile);
                    if (File.Exists(resolved))
                    {
                        try
                        {
                            var text = File.ReadAllText(resolved);
                            parser.TemplateContent = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
                            var ext = Path.GetExtension(resolved);
                            if (!string.IsNullOrEmpty(ext))
                            {
                                parser.TemplateExtension = ext;
                            }

                            parser.UseTemplateFile = null;
                            parser.UseDefaultTemplate = false;
                            changed = true;
                        }
                        catch (Exception e)
                        {
                            Serilog.Log.Warning($"Could not migrate template file '{resolved}' for application '{app.Id}': {e.Message}. Leaving UseTemplateFile in place.");
                        }
                    }
                    else
                    {
                        Serilog.Log.Warning($"Template file '{resolved}' referenced by application '{app.Id}' does not exist; leaving UseTemplateFile in place for review.");
                    }
                }
                else if (parser.UseDefaultTemplate)
                {
                    parser.TemplateContent = UrlParser.DefaultRdpUrlParser.GetDefaultTemplateText();
                    parser.TemplateExtension = ".rdp";
                    parser.UseDefaultTemplate = false;
                    changed = true;
                }
            }

            return changed;
        }

        // Repair configurations whose applications lost their inline template (older/hand-edited configs
        // where TemplateContent went missing). The replacement is sourced from the shipped seed defaults
        // by matching application Id, so the template stays the JSON source of truth for that specific
        // application rather than a generic hardcoded fallback. Only applications that actually consume a
        // generated file are repaired; a client that builds its whole command line from tokens (freerdp)
        // legitimately has no template. Returns true if anything changed.
        internal static bool RepairMissingTemplatesFromSeed(ScalusConfig config, List<ApplicationConfig> seedApps)
        {
            if (config?.Applications == null || seedApps == null || seedApps.Count == 0)
            {
                return false;
            }

            var changed = false;
            foreach (var app in config.Applications)
            {
                var parser = app?.Parser;
                if (parser == null || parser.HasTemplate || !UsesGeneratedFile(app))
                {
                    continue;
                }

                ParserConfig seedParser = null;
                foreach (var seed in seedApps)
                {
                    if (seed?.Parser != null &&
                        string.Equals(seed.Id, app.Id, StringComparison.OrdinalIgnoreCase) &&
                        seed.Parser.HasTemplate)
                    {
                        seedParser = seed.Parser;
                        break;
                    }
                }

                if (seedParser == null)
                {
                    Serilog.Log.Warning(
                        $"Application '{app.Id}' passes a generated file but has no template, and no shipped default with a matching Id was found to restore it.");
                    continue;
                }

                parser.TemplateContent = seedParser.TemplateContent;
                if (string.IsNullOrEmpty(parser.TemplateExtension))
                {
                    parser.TemplateExtension = string.IsNullOrEmpty(seedParser.TemplateExtension)
                        ? ".rdp"
                        : seedParser.TemplateExtension;
                }

                changed = true;
                Serilog.Log.Warning(
                    $"Restored the missing template for application '{app.Id}' from the shipped defaults.");
            }

            return changed;
        }

        protected ScalusConfig Load(string path)
        {
            ValidationErrors = new List<string>();
            var config = new ScalusConfig();

            if (!File.Exists(path))
            {
                ValidationErrors.Add($"Missing config file: {path}");
            }
            else
            {
                var configJson = File.ReadAllText(path);
                (_, config) = Validate(configJson);
            }

            if (ValidationErrors.Count > 0)
            {
                Serilog.Log.Error($"**** Validation of {configFile} failed");
                Serilog.Log.Error($"*** Validation errors: {string.Join(", ", ValidationErrors)}");
            }

            MigrateLegacyTemplates(config);
            return config;
        }

        private static string ResolveConfigTokens(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value
                .Replace("%AppData%", ConfigurationManager.ProdAppPath, StringComparison.OrdinalIgnoreCase)
                .Replace("%Home%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.OrdinalIgnoreCase)
                .Replace("%TempPath%", Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool UsesGeneratedFile(ApplicationConfig app)
        {
            return ReferencesGeneratedFile(app.Args) ||
                (app.Parser != null && ReferencesGeneratedFile(app.Parser.PostProcessingArgs));
        }

        private static bool ReferencesGeneratedFile(List<string> values)
        {
            if (values == null)
            {
                return false;
            }

            foreach (var value in values)
            {
                if (value != null && value.Contains("%GeneratedFile%", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
