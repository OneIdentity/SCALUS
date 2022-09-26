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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
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

#if COMMUNITY_EDITION
                scalusConfig.Edition = Edition.Community;
#else
                scalusConfig.Edition = Edition.Supported;
#endif

                return scalusConfig;
            }

            set
            {
                scalusConfig = value;
            }
        }

        public ScalusConfig GetConfiguration()
        {
            return Config;
        }

        public (bool, ScalusConfig) Validate(string json, bool strict = false)
        {
            var config = new ScalusConfig();
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                MissingMemberHandling = strict ? MissingMemberHandling.Error : MissingMemberHandling.Ignore,
            };
            ValidationErrors = new List<string>();
            try
            {
                config = JsonConvert.DeserializeObject<ScalusConfig>(json, serializerSettings);
            }
            catch (Exception e)
            {
                ValidationErrors.Add($"Error deserialising json configuration:{e.Message}");
            }

            try
            {
                config?.Validate(ValidationErrors, false);
            }
            catch (Exception e)
            {
                ValidationErrors.Add($"Error validating json configuration:{e.Message}");
                return (false, config);
            }

            return (ValidationErrors.Count == 0, config);
        }

        protected ScalusConfig Load(string path)
        {
            ValidationErrors = new List<string>();
            var config = new ScalusConfig();

            if (!File.Exists(path))
            {
                ValidationErrors.Add($"Missing config file:{path}");
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

            return config;
        }
    }
}
