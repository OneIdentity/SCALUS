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

namespace OneIdentity.Scalus.Unregister
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using OneIdentity.Scalus.Util;

    internal class Application : IApplication
    {
        private static readonly string[] BuiltInProtocols = { "ssh", "rdp", "telnet" };

        public Application(
            Options options,
            IRegistration registration,
            IUserInteraction userInteraction,
            IScalusApiConfiguration configuration,
            IEnumerable<IProtocolRegistrar> registrars)
        {
            Options = options;
            Registration = registration;
            UserInteraction = userInteraction;
            Configuration = configuration;
            Registrars = registrars;
        }

        private Options Options { get; }

        private IRegistration Registration { get; }

        private IUserInteraction UserInteraction { get; }

        private IScalusApiConfiguration Configuration { get; }

        private IEnumerable<IProtocolRegistrar> Registrars { get; }

        public int Run()
        {
            if (Options.Quiet)
            {
                UserInteraction.Silence();
            }

            var requestedProtocols = Options.Protocols?.ToArray();
            var protocols = requestedProtocols != null && requestedProtocols.Length > 0
                ? requestedProtocols
                : GetProtocols(
                    GetConfiguredProtocols(),
                    GetRegisteredProtocols());

            Registration.UnRegister(protocols, Options.RootMode, Options.UseSudo);
            if (ShouldRemoveConfiguration(
                Options.RemoveConfiguration,
                Options.PromptRemoveConfiguration,
                WindowsConfigurationRemovalPrompt.Confirm))
            {
                DeleteUserSettings(
                    ConfigurationManager.ScalusJson,
                    Path.Combine(ConfigurationManager.ProdAppPath, UiWindowSettings.FileName));
            }

            return 0;
        }

        internal static bool ShouldRemoveConfiguration(
            bool removeConfiguration,
            bool promptRemoveConfiguration,
            Func<bool> confirm) =>
            removeConfiguration || (promptRemoveConfiguration && confirm());

        internal static void DeleteUserSettings(string configurationPath, string windowSettingsPath)
        {
            DeleteFile(configurationPath);
            DeleteFile(windowSettingsPath);
        }

        internal static string[] GetProtocols(
            IEnumerable<string> configuredProtocols,
            IEnumerable<string> registeredProtocols = null) =>
            BuiltInProtocols
                .Concat(configuredProtocols ?? Array.Empty<string>())
                .Concat(registeredProtocols ?? Array.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        private IEnumerable<string> GetConfiguredProtocols() =>
            Options.RootMode
                ? Array.Empty<string>()
                : Configuration.GetConfiguration().Protocols?.Select(p => p.Protocol);

        private IEnumerable<string> GetRegisteredProtocols()
        {
            foreach (var registrar in Registrars)
            {
                registrar.RootMode = Options.RootMode;
                if (registrar is IRegisteredProtocolSource source)
                {
                    foreach (var protocol in source.GetRegisteredProtocols())
                    {
                        yield return protocol;
                    }
                }
            }
        }

        private static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
