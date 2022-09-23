// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScalusConfig.cs" company="One Identity Inc.">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ScalusConfig
    {
        private static Dictionary<string, string> dtoPropertyDescription = new Dictionary<string, string>
        {
            { nameof(Protocols), "The list of protocols configured for scalus" },
            { nameof(Applications), "The list of applications available to use" },
        };

        public List<ProtocolMapping> Protocols { get; set; }

        public List<ApplicationConfig> Applications { get; set; }

        public static Dictionary<string, string> DtoPropertyDescription => dtoPropertyDescription.Append(ProtocolMapping.DtoPropertyDescription).Append(ApplicationConfig.DtoPropertyDescription);

        public void Validate(List<string> errors, bool log = true)
        {
            if (Protocols != null)
            {
                foreach (var one in Protocols)
                {
                    one.Validate(errors);
                    if (string.IsNullOrEmpty(one.AppId))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(one.Protocol))
                    {
                        continue;
                    }

                    var foundapp = Applications?.FirstOrDefault(a =>
                        !string.IsNullOrEmpty(a.Id) && a.Id.Equals(one.AppId, StringComparison.OrdinalIgnoreCase));

                    if (foundapp == null)
                    {
                        errors.Add($"Protocol:{one.Protocol} is mapped to undefined application id:{one.AppId}");
                    }
                    else if (string.IsNullOrEmpty(foundapp.Protocol))
                    {
                        errors.Add($"Protocol:{one.Protocol} is mapped to application:{foundapp.Name}({one.AppId}) which has no protocol defined");
                    }
                    else if (!foundapp.Protocol.Equals(one.Protocol, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Protocol:{one.Protocol} is mapped to application:{foundapp.Name}({one.AppId}) which has a different protocol defined: {foundapp.Protocol}");
                    }
                }

                var list = Protocols.Select(one => one.Protocol).ToList();
                if (Protocols.Count != list.Distinct().Count())
                {
                    errors.Add($"Protocols list must not contain multiple definitions for a protocol string");
                }
            }

            if (Applications != null)
            {
                foreach (var one in Applications)
                {
                    one.Validate(errors);
                }

                var list = Applications.Select(one => one.Id).ToList();
                if (Applications.Count != list.Distinct().Count())
                {
                    errors.Add($"Application Ids must be unique");
                }
            }

            if (!log)
            {
                return;
            }

            if (errors.Count > 0)
            {
                Serilog.Log.Error($"**** Failed to validate scalus configuration");
                Serilog.Log.Error($"*** Validation errors: {string.Join(", ", errors)}");
            }
        }
    }

    public static class Extensions
    {
        public static Dictionary<TK, TV> Append<TK, TV>(this Dictionary<TK, TV> first, Dictionary<TK, TV> second)
        {
            var res = new Dictionary<TK, TV>(first);
            List<KeyValuePair<TK, TV>> pairs = second.ToList();
            pairs.ForEach(pair => res[pair.Key] = pair.Value);
            return res;
        }
    }
}
