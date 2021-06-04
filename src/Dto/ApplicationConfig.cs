using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace scalus.Dto
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Platform
    {
        Windows =0,
        Linux=1,
        Mac=2
    }

    public class ApplicationConfig
    {
        [JsonRequired]
        public string Id { get; set; }
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

        private static readonly Dictionary<string, string> _dtoPropertyDescription = new Dictionary<string, string>
        {
            {nameof(Id),"The unique identifier for an application that can be configured to launch for the selected protocol"},
            {nameof(Name),"User-friendly name for the application"},
            {nameof(Description),"Optional description of this application"},
            {nameof(Platforms),$"The list of platforms supported for this application. Valid values are: {string.Join(',',Enum.GetValues(typeof(Platform)).Cast<Platform>())}"},
            {nameof(Protocol),"The protocol supported for this application"},
            {nameof(Parser),$"The parser that will be used to interpret the URL. The available values are: {string.Join(',',ProtocolHandlerFactory.GetSupportedParsers())}"},
            {nameof(Exec),"The full path to the command to run. This can contain any of the supported tokens."},
            {nameof(Args),"The list of arguments to pass to the command. This can contain any of the supported tokens."}
        };
        public static Dictionary<string, string> DtoPropertyDescription => _dtoPropertyDescription.Append(ParserConfig.DtoPropertyDescription);

        public void Validate(List<string> errors)
        {
            if (Platforms.Count == 0)
            { 
                errors.Add($"Application:{Name}({Id}) does not have any platforms defined.");
            }
            Parser?.Validate(errors);
        }
    }
}
