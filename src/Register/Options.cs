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

namespace OneIdentity.Scalus.Register
{
    using System.Collections.Generic;
    using CommandLine;

    [Verb("register", HelpText = "Register SCALUS to handle URLs")]
    public class Options : IVerb
    {
        [Option('f', "force", Required = false, HelpText = "Overwrite an existing registration")]
        public bool Force { get; set; }

        [Option('p', "protocols", Required = false, HelpText = "A space-separated list of URL protocols to handle (Default: ssh rdp telnet)", Default = new string[] { "ssh", "rdp", "telnet" })]
        public IEnumerable<string> Protocols { get; set; }

        [Option('r', "root", Required = false, HelpText = "Update system files as well as user files")]
        public bool RootMode { get; set; }

        [Option('s', "sudo", Required = false, HelpText = "use (passwordless) sudo to update system files on supported platforms")]
        public bool UseSudo { get; set; }
    }
}
