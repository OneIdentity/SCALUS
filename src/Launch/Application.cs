// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Launch
{
    using System;
    using System.Linq;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Platform;
    using OneIdentity.Scalus.Util;

    internal class Application : IApplication
    {
        public Application(Launch.Options options, IOsServices osServices, IScalusConfiguration config)
        {
            this.Options = options;
            Config = config;
            OsServices = osServices;
        }

        private Launch.Options Options { get; }

        private IScalusConfiguration Config { get; }

        private IOsServices OsServices { get; }

        public int Run()
        {
            Serilog.Log.Debug($"Dispatching URL: {Options.Url}");

            try
            {
                using var handler = Config.GetProtocolHandler(Options.Url);
                if (handler == null)
                {
                    // We are the registered application, but we can't parse the config
                    // or nothing is configured, show an error somehow
                    OsServices.OpenText($"SCALUS configuration does not provide a method to handle the URL: {Options.Url}");
                    return 1;
                }

                handler.Run(Options.Preview);
                return 0;
            }
            catch (Exception ex)
            {
                HandleLaunchError(ex, Options.Url);
            }

            return 1;
        }

        private void HandleLaunchError(Exception ex, string url)
        {
            ApplicationConfig application = null;
            var scalusJsonPath = ConfigurationManager.ScalusJson;

            try
            {
                application = GetApplicationForProtocol(Config.GetConfiguration(), GetProtocol(url));
            }
            catch (Exception e)
            {
                OsServices.ShowMessage($"Failed to read config file:{scalusJsonPath}:{e.Message}");
            }

            var msg =
$@"[SCALUS]: Failed to launch registered URL handler.
  URL:           {url}
  Error:         {ex.Message}

  ApplicationId: {application?.Id ?? "<none>"}
  Command:       {application?.Exec ?? "<none>"}
  Args:          {(application?.Args != null ? string.Join(" ", application?.Args) : "<none>")}

  Config File:   {scalusJsonPath}  

Check the configuration for this URL protocol.";
            OsServices.OpenText(msg);
            Serilog.Log.Error(msg, ex);
        }

        private static string GetProtocol(string url)
        {
            var protocolSeparatorIndex = url.IndexOf("://");
            if (protocolSeparatorIndex == -1)
            {
                return string.Empty;
            }

            return url.Substring(0, protocolSeparatorIndex);
        }

        private static ApplicationConfig GetApplicationForProtocol(ScalusConfig config, string protocol)
        {
            var application = config.Protocols.FirstOrDefault(x => x.Protocol == protocol);
            if (application == null)
            {
                return null;
            }

            return config.Applications.FirstOrDefault(x => x.Id == application.AppId);
        }
    }
}
