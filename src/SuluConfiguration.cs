using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sulu.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu
{

    class SuluConfigurationBase
    {
        protected SuluConfig Config { get; set; } = new SuluConfig();

        protected SuluConfigurationBase()
        {
            Load();
        }
        
        protected void Load()
        {
            var configFile = Path.Combine(Constants.GetBinaryDir(), "sulu.json");
            if (!File.Exists(configFile))
            {
                Serilog.Log.Warning($"config file not found at: {configFile}");
                return;
            }

            var configJson = File.ReadAllText(configFile);

            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var json = JsonConvert.SerializeObject(configJson, serializerSettings);
            Config = JsonConvert.DeserializeObject<SuluConfig>(configJson);
        }
    }

    class SuluApiConfiguration : SuluConfigurationBase, ISuluApiConfiguration
    {
        public SuluApiConfiguration() : base() { }

        public SuluConfig GetConfiguration()
        {
            return Config;
        }

        public SuluConfig SaveConfiguration(SuluConfig configuration)
        {
            throw new NotImplementedException();
        }
    }


    class SuluConfiguration : SuluConfigurationBase, ISuluConfiguration
    {
        IProtocolHandlerFactory ProtocolHandlerFactory { get; }

        public SuluConfiguration(IProtocolHandlerFactory protocolHandlerFactory) : base()
        {
            ProtocolHandlerFactory = protocolHandlerFactory;
        }

        public IProtocolHandler GetProtocolHandler(string uri)
        {
            // var manually parse out the protocol
            var index = uri.IndexOf("://");
            var protocol = "";
            if (index >= 0)
            {
                protocol = uri.Substring(0, index);
            }
            var protocolMap = Config.Map.FirstOrDefault(x => string.Equals(x.Protocol, protocol, StringComparison.OrdinalIgnoreCase));
            if(protocolMap == null)
            {
                Serilog.Log.Warning($"There is no application configured for protocol {protocol}");
                return null;
            }

            var protocolConfig = Config.Protocols.FirstOrDefault(x => string.Equals(x.Id, protocolMap.Id, StringComparison.OrdinalIgnoreCase));
            if (protocolConfig == null)
            {
                Serilog.Log.Warning($"Mapped protocol configuration {protocolMap.Id} for {protocol} was not found in sulu.json.");
                return null;
            }

            return ProtocolHandlerFactory.Create(uri, protocolConfig);
        }
    }
}
