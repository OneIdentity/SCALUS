// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScalusApiConfiguration.cs" company="One Identity Inc.">
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
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Util;

    internal class ScalusApiConfiguration : ScalusConfigurationBase, IScalusApiConfiguration
    {
        public ScalusApiConfiguration()
            : base()
        {
        }

        public ScalusApiConfiguration(string json)
        {
        }

        public List<string> SaveConfiguration(ScalusConfig configuration)
        {
            ValidationErrors = new List<string>();
            MigrateLegacyTemplates(configuration);

            // TODO: Save the file and keep the comments and formatting
            // I think this can be done by switching the config file to
            // json5 and adding a parser for that where we would read the
            // config as json5 replace the values from the incoming config
            // object and then write it out again

            if (ValidateAndSave(configuration))
            {
                Config = Load(configFile);
            }

            if (ValidationErrors.Count > 0)
            {
                Serilog.Log.Error($"**** Validation of {configFile} failed");
                Serilog.Log.Error($"*** Validation errors: {string.Join(", ", ValidationErrors)}");
            }

            return ValidationErrors;
        }

        // Migrates any legacy template fields on disk into inline TemplateContent and re-saves, so the
        // change is visible in the file for CLI-only users who never open the configuration UI. No-op
        // when the config is already migrated.
        public bool MigrateOnDisk()
        {
            if (!File.Exists(configFile))
            {
                return false;
            }

            var (_, config) = Validate(File.ReadAllText(configFile));
            if (config == null || !MigrateLegacyTemplates(config))
            {
                return false;
            }

            Serilog.Log.Information("Migrating legacy templates into the configuration file");
            SaveConfiguration(config);
            return ValidationErrors.Count == 0;
        }

        private bool ValidateAndSave(ScalusConfig configuration, bool save = true)
        {
            try
            {
                var json = ScalusJson.Serialize(configuration);
                var ok = false;
                (ok, _) = Validate(json);
                if (ok)
                {
                    if (save)
                    {
                        File.WriteAllText(configFile, json);
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                ValidationErrors.Add(e.Message);
            }

            return false;
        }
    }
}
