using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using scalus.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using scalus.Util;

namespace scalus
{

    class ScalusConfigurationBase
    {
        protected ScalusConfig Config { get; set; } = new ScalusConfig();
        public List<string> ValidationErrors { get; protected set;  }= new List<string>();
        protected string _configFile = ConfigurationManager.ScalusJson;
        protected ScalusConfigurationBase()
        {
            Load();
        }


        public ScalusConfig GetConfiguration()
        {
            return Config;
        }

        protected void Load()
        {
            ValidationErrors = new List<string>();

            if (!File.Exists(_configFile))
            {
                ValidationErrors.Add($"Missing config file:{_configFile}");
            }
            else
            {
                var configJson = File.ReadAllText(_configFile);
                Validate(configJson);
            }
            if (ValidationErrors.Count > 0)
            {
                Serilog.Log.Error($"**** Validation of {_configFile} failed");
                Serilog.Log.Error($"*** Validation errors: {string.Join(", ", ValidationErrors)}");
            }
        }

        public bool Validate(string json)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            ValidationErrors = new List<string>();
            try
            {
                Config = JsonConvert.DeserializeObject<ScalusConfig>(json, serializerSettings);
            }
            catch (Exception e)
            {
                ValidationErrors.Add($"Error deserialising json configuration:{e.Message}");
            }
            try
            {
                Config?.Validate(ValidationErrors, false);
            }
            catch (Exception e)
            {
                ValidationErrors.Add($"Error validating json configuration:{e.Message}");
                return false;
            }
            return (ValidationErrors.Count == 0);
        }

    }


    class ScalusApiConfiguration : ScalusConfigurationBase, IScalusApiConfiguration
    {
        public ScalusApiConfiguration() : base() { }

        public ScalusApiConfiguration(string json)
        {
        }

        public ScalusConfig SaveConfiguration(ScalusConfig configuration)
        {
            ValidationErrors = new List<string>();

            // TODO: Save the file and keep the comments and formatting
            // I think this can be done by switching the config file to
            // json5 and adding a parser for that where we would read the
            // config as json5 replace the values from the incoming config
            // object and then write it out again

            if (Validate(configuration))
            {
                Load();
            }
            if (ValidationErrors.Count > 0)
            {
                Serilog.Log.Error($"**** Validation of {_configFile} failed");
                Serilog.Log.Error($"*** Validation errors: {string.Join(", ", ValidationErrors)}");
            }

            return Config;
        }


        private bool Validate(ScalusConfig configuration, bool save = true)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented
            };

            try
            {
                var json = JsonConvert.SerializeObject(configuration, serializerSettings);
                if (Validate(json))
                {
                    if (save)
                    {
                        File.WriteAllText(_configFile, json);
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


    class ScalusConfiguration : ScalusConfigurationBase, IScalusConfiguration
    {
        IProtocolHandlerFactory ProtocolHandlerFactory { get; }

        public ScalusConfiguration(IProtocolHandlerFactory protocolHandlerFactory) : base()
        {
            ProtocolHandlerFactory = protocolHandlerFactory;
        }

        public IProtocolHandler GetProtocolHandler(string uri)
        {
            Serilog.Log.Information($"Checking configuration for url:{uri}");
            // var manually parse out the protocol
            var index = uri.IndexOf("://");
            var protocol = "";
            if (index >= 0)
            {
                protocol = uri.Substring(0, index);
            }

            if (string.IsNullOrEmpty(protocol))
            {
                Serilog.Log.Warning($"No protocol was specified in the url:{uri}");
                return null;
            }
            var protocolMap = Config?.Protocols?.FirstOrDefault(x => string.Equals(x.Protocol, protocol, StringComparison.OrdinalIgnoreCase));
            if(protocolMap == null)
            {
                Serilog.Log.Warning($"There is no application configured for protocol {protocol}");
                // TODO: Restart in UI mode
                return null;
            }

            var protocolConfig = Config?.Applications?.FirstOrDefault(x => string.Equals(x.Id, protocolMap.AppId, StringComparison.OrdinalIgnoreCase));
            if (protocolConfig == null)
            {
                Serilog.Log.Warning($"Application configuration '{protocolMap.AppId}' for '{protocol}' was not found in {ConfigurationManager.ScalusJson} config.");
                // TODO: Restart in UI mode
                return null;
            }

            return ProtocolHandlerFactory.Create(uri, protocolConfig);
        }
    }
}
