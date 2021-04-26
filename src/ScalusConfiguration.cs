using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using scalus.Dto;
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using scalus.Util;

namespace scalus
{

    class ScalusConfigurationBase
    {
        protected ScalusConfig Config { get; set; } = new ScalusConfig();

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
            var configFile = ConfigurationManager.ScalusJson ;
            if (!File.Exists(configFile))
            {
                Serilog.Log.Warning($"config file not found at: {configFile}");
                return;
            }

            var configJson = File.ReadAllText(configFile);

            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var json = JsonConvert.SerializeObject(configJson, serializerSettings);
            try{
                Config = JsonConvert.DeserializeObject<ScalusConfig>(configJson);
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, $"Failed to parse file:{configJson}: {e.Message}");
            }
        }
    }

    class ScalusApiConfiguration : ScalusConfigurationBase, IScalusApiConfiguration
    {
        public ScalusApiConfiguration() : base() { }

        

        public ScalusConfig SaveConfiguration(ScalusConfig configuration)
        {
            // TODO: Save the file and keep the comments and formatting
            // I think this can be done by switching the config file to
            // json5 and adding a parser for that where we would read the
            // config as json5 replace the values from the incoming config
            // object and then write it out again

            Save(configuration);
            Load();
            return Config;
        }

        private void Save(ScalusConfig configuration)
        {
            var configFile = ConfigurationManager.ScalusJson ;
            
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Formatting = Formatting.Indented;
            
            var json = JsonConvert.SerializeObject(configuration, serializerSettings);
            File.WriteAllText(configFile, json);
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
            var protocolMap = Config.Protocols.FirstOrDefault(x => string.Equals(x.Protocol, protocol, StringComparison.OrdinalIgnoreCase));
            if(protocolMap == null)
            {
                Serilog.Log.Warning($"There is no application configured for protocol {protocol}");
                // TODO: Restart in UI mode
                return null;
            }

            var protocolConfig = Config.Applications.FirstOrDefault(x => string.Equals(x.Id, protocolMap.AppId, StringComparison.OrdinalIgnoreCase));
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
