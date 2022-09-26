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

namespace OneIdentity.Scalus.Launch
{
    using CommandLine;

    [Verb("launch", HelpText = "Launch an app configured for the specified URL")]
    public class Options : IVerb
    {
        [Option('u', "url", Required = true, HelpText = "The URL to launch.")]
        public string Url { get; set; }

        [Option('p', "preview", Required = false, HelpText = "Show me what will launch, but dont run it. This will also report the token values and show the contents of the generated file, if applicable.")]
        public bool Preview { get; set; }
    }
}
