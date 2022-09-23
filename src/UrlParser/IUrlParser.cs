// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IUrlParser.cs" company="One Identity Inc.">
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
    using System.Diagnostics;
    using OneIdentity.Scalus.Platform;
    using static OneIdentity.Scalus.Dto.ParserConfigDefinitions;

    public interface IUrlParser : IDisposable
    {
        IDictionary<Token, string> Parse(string url);

        void PostExecute(Process process);

        List<string> ReplaceTokens(List<string> args);

        string ReplaceTokens(string arg);

        void PreExecute(IOsServices services);
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class ParserName : Attribute
    {
        private string name;

        public ParserName(string name)
        {
            this.name = name;
        }

        public string GetName()
        {
            return name;
        }
    }
}
