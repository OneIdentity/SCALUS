using scalus.Dto;
using scalus.Platform;
using scalus.UrlParser;

namespace scalus
{
    class ProtocolHandlerFactory : IProtocolHandlerFactory
    {

        IOsServices OsServices { get; }

        public ProtocolHandlerFactory(IOsServices osServices)
        {
            OsServices = osServices;
        }

        public IProtocolHandler Create(string uri, ApplicationConfig config)
        {
            switch (config.Parser.Id)
            {
                // TODO: Resolve by Id from autofac
                case "rdp":
                    return new ProtocolHandler(uri, new DefaultRdpUrlParser(config.Parser), config, OsServices);
                case "rdp-file":
                    return new ProtocolHandler(uri, new RdpFileUrlParser(config.Parser), config, OsServices);
                case "ssh":
                    return new ProtocolHandler(uri, new DefaultSshUrlParser(config.Parser), config, OsServices);
            }
            return null;
        }
    }
}
