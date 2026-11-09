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
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.UrlParser;

    internal class ProtocolHandlerFactory : IProtocolHandlerFactory
    {
        // Explicit registry of the built-in URL parsers, keyed by their parser id. Keeping this
        // static (instead of scanning loaded assemblies with reflection and Activator.CreateInstance)
        // is what lets Scalus.Core compile cleanly under trimming / NativeAOT. The keys match the
        // [ParserName] attributes still declared on each parser class.
        private static readonly IReadOnlyDictionary<string, Func<ParserConfig, IUrlParser>> Parsers =
            new Dictionary<string, Func<ParserConfig, IUrlParser>>(StringComparer.Ordinal)
            {
                ["rdp"] = config => new DefaultRdpUrlParser(config),
                ["ssh"] = config => new DefaultSshUrlParser(config),
                ["telnet"] = config => new DefaultTelnetUrlParser(config),
                ["url"] = config => new UrlParser.UrlParser(config),
            };

        public ProtocolHandlerFactory(IOsServices osServices, ITerminalResolver terminalResolver)
        {
            OsServices = osServices;
            TerminalResolver = terminalResolver;
        }

        private IOsServices OsServices { get; }

        private ITerminalResolver TerminalResolver { get; }

        public static List<string> GetSupportedParsers() => new (Parsers.Keys);

        public IProtocolHandler Create(string uri, ApplicationConfig config, string preferredTerminal = null)
        {
            var parserId = config.Parser.ParserId;
            if (!string.IsNullOrEmpty(parserId) && Parsers.TryGetValue(parserId, out var factory))
            {
                Serilog.Log.Information($"Found parser: {parserId}");
                return new ProtocolHandler(uri, factory(config.Parser), config, OsServices, TerminalResolver, preferredTerminal);
            }

            // default to url handler
            Serilog.Log.Information($"No specific parser found for: {parserId}, defaulting to urlParser");
            return new ProtocolHandler(uri, new UrlParser.UrlParser(config.Parser), config, OsServices, TerminalResolver, preferredTerminal);
        }
    }
}
