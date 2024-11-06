// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScalusConfiguration.cs" company="One Identity Inc.">
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
    using System.Linq;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Util;

    internal class ScalusConfiguration : ScalusConfigurationBase, IScalusConfiguration
    {
        public ScalusConfiguration(IProtocolHandlerFactory protocolHandlerFactory)
            : base()
        {
            ProtocolHandlerFactory = protocolHandlerFactory;
        }

        private IProtocolHandlerFactory ProtocolHandlerFactory { get; }

        public IProtocolHandler GetProtocolHandler(string uri)
        {
            Serilog.Log.Information($"Checking configuration for url:{uri}");
            // var manually parse out the protocol
            var index = uri.IndexOf("://", StringComparison.Ordinal);
            var protocol = string.Empty;
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
            if (protocolMap == null)
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

        public ScalusConfig GetConfiguration(string path = null)
        {
            return Load(path ?? configFile);
        }
    }
}
