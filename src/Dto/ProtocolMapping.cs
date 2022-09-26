// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProtocolMapping.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Dto
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;

    public class ProtocolMapping
    {
        private static readonly Dictionary<string, string> DtoPropertyDescriptionValue = new Dictionary<string, string>
        {
            { nameof(Protocol), "The protocol string that will be registered for scalus" },
            { nameof(AppId), "The unique id of the application that will be used to launch a URL for this protocol" },
        };

        public static Dictionary<string, string> DtoPropertyDescription => DtoPropertyDescriptionValue;

        [JsonRequired]
        public string Protocol { get; set; }

        public string AppId { get; set; }

        public static bool ValidateProtocol(string val, out string err)
        {
            err = null;
            if (string.IsNullOrEmpty(val))
            {
                err = "Protocol name must be configured";
                return false;
            }

            if (Regex.IsMatch(val, "^[a-zA-Z][a-zA-Z0-9-+.]+$"))
            {
                return true;
            }

            err = $"Protocol name contains invalid chars:{val}";
            return false;
        }

        public void Validate(List<string> errors)
        {
            if (!ValidateProtocol(Protocol, out string err))
            {
                errors.Add(err);
            }
        }
    }
}