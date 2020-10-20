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
    class SuluConfiguration : ISuluConfiguration
    {
        SuluConfig Config { get; set; } = new SuluConfig();
        IProtocolHandlerFactory ProtocolHandlerFactory { get; }

        public SuluConfiguration(IProtocolHandlerFactory protocolHandlerFactory)
        {
            ProtocolHandlerFactory = protocolHandlerFactory;
            Load();
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
            var protocolConfig = Config.Protocols.FirstOrDefault(x => string.Equals(x.Name, protocol, StringComparison.OrdinalIgnoreCase));
            if (protocolConfig == null) return null;

            return ProtocolHandlerFactory.Create(uri, protocolConfig);
        }

        private void Load()
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

    
}
