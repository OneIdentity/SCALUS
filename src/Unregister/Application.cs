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
    using OneIdentity.Scalus.Util;

    internal class Application : IApplication
    {
        public Application(Options options, IRegistration registration, IUserInteraction userInteraction)
        {
            Options = options;
            Registration = registration;
            UserInteraction = userInteraction;
        }

        private Options Options { get; }

        private IRegistration Registration { get; }

        private IUserInteraction UserInteraction { get; }

        public int Run()
        {
            if (Options.Quiet)
            {
                UserInteraction.Silence();
            }

            Registration.UnRegister(Options.Protocols, Options.RootMode, Options.UseSudo);

            var shouldDelete = Options.DeleteConfig;
            if (!shouldDelete && Options.PromptDeleteConfig)
            {
                shouldDelete = UninstallPrompt.ConfirmDeleteConfiguration();
            }

            if (shouldDelete)
            {
                var (removed, path) = ConfigurationManager.DeleteConfiguration();
                UserInteraction.Message(removed
                    ? $"Removed SCALUS configuration data: {path}"
                    : "No SCALUS configuration data to remove.");
            }

            return 0;
        }
    }
}
