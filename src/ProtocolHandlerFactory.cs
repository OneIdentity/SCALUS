using scalus.Dto;
using scalus.Platform;
using scalus.UrlParser;
using System;
using System.Linq;
using System.Reflection;

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
 
            var tlist = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => typeof(IUrlParser).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
               .ToList();

            foreach (var t in tlist)
            {
                if ((t.GetCustomAttribute(typeof(ParserName))) is ParserName c && 
                    (c.GetName().Equals(config.Parser.Id)))
                {
                    Serilog.Log.Information($"Found parser:{t.Name}");
                    return new ProtocolHandler(uri, (IUrlParser)Activator.CreateInstance(t, config.Parser), config, OsServices);
                }
            }
            //default to url handler
            Serilog.Log.Information($"No specific parser found for:{config.Parser.Id}, defaulting to urlParser");
            return new ProtocolHandler(uri, new UrlParser.UrlParser(config.Parser), config, OsServices);
        }
    }
}
