// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultTelnetUrlParser.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.UrlParser
{
    using System.Collections.Generic;
    using OneIdentity.Scalus.Dto;
    using static OneIdentity.Scalus.Dto.ParserConfigDefinitions;

    [ParserName("telnet")]
    internal class DefaultTelnetUrlParser : DefaultSshUrlParser
    {
        // Identical to the ssh parser
        public DefaultTelnetUrlParser(ParserConfig config)
            : base(config)
        {
            FileExtension = ".telnet";
        }

        public DefaultTelnetUrlParser(ParserConfig config, IDictionary<Token, string> dictionary)
            : this(config)
        {
            if (dictionary != null)
            {
                Dictionary = dictionary;
            }
        }
    }
}
