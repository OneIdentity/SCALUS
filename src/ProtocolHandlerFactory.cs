using Sulu.Dto;
using Sulu.Platform;
using Sulu.UrlParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu
{
    class ProtocolHandlerFactory : IProtocolHandlerFactory
    {

        IOsServices OsServices { get; }

        public ProtocolHandlerFactory(IOsServices osServices)
        {
            OsServices = osServices;
        }

        public IProtocolHandler Create(string uri, ProtocolConfig config)
        {
            if(config.Parser.Type == "builtin")
            {
                switch (config.Parser.Id)
                {
                    // TODO: Resolve by Id from autofac
                    case "rdp":
                        return new ProtocolHandler(uri, new DefaultRdpUrlParser(), config, OsServices);
                    case "rdp-file":
                        return new ProtocolHandler(uri, new RdpFileUrlParser(), config, OsServices);
                    case "ssh":
                        return new ProtocolHandler(uri, new DefaultSshUrlParser(), config, OsServices);
                }
            }
            return null;
        }
    }
}
