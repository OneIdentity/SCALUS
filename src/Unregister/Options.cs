// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Options.cs" company="One Identity Inc.">
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
    using System.CommandLine;

    public class Options : IVerb
    {
        public IEnumerable<string> Protocols { get; set; }

        public bool RootMode { get; set; }

        public bool UseSudo { get; set; }

        public bool Quiet { get; set; }

        public bool RemoveConfiguration { get; set; }

        public Command CreateCommand(Action<object> onParsed)
        {
            var protocols = new Option<string[]>("--protocols", "-p")
            {
                Description = "A space-separated list of URL protocols to unregister (Default: all configured protocols)",
                AllowMultipleArgumentsPerToken = true,
            };
            var root = new Option<bool>("--root", "-r") { Description = "Update system files as well as user files" };
            var sudo = new Option<bool>("--sudo", "-s") { Description = "use (passwordless) sudo to update system files on supported platforms" };
            var quiet = new Option<bool>("--quiet", "-q") { Hidden = true };
            var removeConfiguration = new Option<bool>("--remove-configuration") { Hidden = true };
            var command = new Command("unregister", "Unregister SCALUS for URL handling");
            command.Add(protocols);
            command.Add(root);
            command.Add(sudo);
            command.Add(quiet);
            command.Add(removeConfiguration);
            command.SetAction(result =>
            {
                onParsed(new Options
                {
                    Protocols = result.GetValue(protocols),
                    RootMode = result.GetValue(root),
                    UseSudo = result.GetValue(sudo),
                    Quiet = result.GetValue(quiet),
                    RemoveConfiguration = result.GetValue(removeConfiguration),
                });
                return 0;
            });
            return command;
        }
    }
}
