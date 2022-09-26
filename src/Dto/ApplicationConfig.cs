// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationConfig.cs" company="One Identity Inc.">
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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Platform
    {
        Windows = 0,
        Linux = 1,
        Mac = 2,
    }

    public class ApplicationConfig
    {
        private static readonly Dictionary<string, string> DtoPropertyDescriptionValue = new Dictionary<string, string>
        {
            { nameof(Id), "The unique identifier for an application that can be configured to launch for the selected protocol" },
            { nameof(Name), "User-friendly name for the application" },
            { nameof(Description), "Optional description of this application" },
            { nameof(Platforms), $"The list of platforms supported for this application. Valid values are: {string.Join(',', Enum.GetValues(typeof(Platform)).Cast<Platform>())}" },
            { nameof(Protocol), "The protocol supported for this application" },
            { nameof(Parser), $"The parser that will be used to interpret the URL. The available values are: {string.Join(',', ProtocolHandlerFactory.GetSupportedParsers())}" },
            { nameof(Exec), "The full path to the command to run. This can contain any of the supported tokens." },
            { nameof(Args), "The list of arguments to pass to the command. This can contain any of the supported tokens." },
        };

        public static Dictionary<string, string> DtoPropertyDescription => DtoPropertyDescriptionValue.Append(ParserConfig.DtoPropertyDescription);

        [JsonRequired]
        public string Id { get; set; }

        [JsonRequired]
        public string Name { get; set; }

        public string Description { get; set; }

        [JsonRequired]
        public List<Platform> Platforms { get; set; }

        [JsonRequired]
        public string Protocol { get; set; }

        [JsonRequired]
        public ParserConfig Parser { get; set; }

        [JsonRequired]
        public string Exec { get; set; }

        public List<string> Args { get; set; }

        public void Validate(List<string> errors)
        {
            if (string.IsNullOrEmpty(Id))
            {
                errors.Add("Id must be configured for an application");
            }

            if (string.IsNullOrEmpty(Exec))
            {
                errors.Add($"An Exec must be configured for application:{Name}({Id})");
            }

            if (!ProtocolMapping.ValidateProtocol(Protocol, out string err))
            {
                errors.Add(err);
            }

            if (string.IsNullOrEmpty(Name))
            {
                errors.Add($"Application:{Id} must have a name");
            }

            if (Platforms.Count == 0)
            {
                errors.Add($"Application:{Name}({Id}) does not have any platforms defined.");
            }

            Parser?.Validate(errors);
        }
    }
}
