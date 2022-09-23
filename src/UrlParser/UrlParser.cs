// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlParser.cs" company="One Identity Inc.">
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
    using System;
    using System.Collections.Generic;
    using OneIdentity.Scalus.Dto;

    [ParserName("url")]
    internal class UrlParser : BaseParser
    {
        //This class parses a standard URL into components:

        public UrlParser(ParserConfig config)
            : base(config)
        {
            FileExtension = ".url";
        }

        public UrlParser(ParserConfig config, IDictionary<Token, string> dictionary)
            : this(config)
        {
            if (dictionary != null)
                Dictionary = dictionary;
        }

        public override IDictionary<Token, string> Parse(string url)
        {
            Dictionary = DefaultDictionary();
            Dictionary[Token.OriginalUrl] = url;
            Dictionary[Token.Protocol] = Protocol(url);
            Dictionary[Token.RelativeUrl] = StripProtocol(url);
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri result))
            {
                Parse(result);
            }
            else
            {
                Serilog.Log.Warning($"The string does not appear to be a valid URL: {url}");
            }

            ParseConfig();
            return Dictionary;
        }

        protected override IEnumerable<string> GetDefaultTemplate()
        {
            Serilog.Log.Error("No default template is supported for this parser type");
            throw new Exception("No default template is defined for this parser type");
        }

    }
}
