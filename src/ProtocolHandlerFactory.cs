// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProtocolHandlerFactory.cs" company="One Identity Inc.">
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
    using System.Linq;
    using System.Reflection;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.UrlParser;

    internal class ProtocolHandlerFactory : IProtocolHandlerFactory
    {

        private IOsServices OsServices { get; }

        public ProtocolHandlerFactory(IOsServices osServices)
        {
            OsServices = osServices;
        }

        public static List<string> GetSupportedParsers()
        {
            var tlist = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => typeof(IUrlParser).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
               .ToList();
            var nameList = new List<string>();
            foreach (var t in tlist)
            {
                var name = t.GetCustomAttribute(typeof(ParserName)) as ParserName;
                nameList.Add(name.GetName());
            }

            return nameList;
        }

        public IProtocolHandler Create(string uri, ApplicationConfig config)
        {

            var tlist = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => typeof(IUrlParser).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
               .ToList();

            foreach (var t in tlist)
            {
                if ((t.GetCustomAttribute(typeof(ParserName))) is ParserName c &&
                    (c.GetName().Equals(config.Parser.ParserId)))
                {
                    Serilog.Log.Information($"Found parser:{t.Name}");
                    return new ProtocolHandler(uri, (IUrlParser)Activator.CreateInstance(t, config.Parser), config, OsServices);
                }
            }

            //default to url handler
            Serilog.Log.Information($"No specific parser found for:{config.Parser.ParserId}, defaulting to urlParser");
            return new ProtocolHandler(uri, new UrlParser.UrlParser(config.Parser), config, OsServices);
        }
    }
}
