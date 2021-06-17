using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace scalus.Dto
{
    public class ProtocolMapping
    {
        [JsonRequired]
        public string Protocol { get; set; }
        public string AppId { get; set; }

        private static readonly Dictionary<string, string> _dtoPropertyDescription = new Dictionary<string, string>
        {
            {nameof(Protocol), "The protocol string that will be registered for scalus"},
            {nameof(AppId), "The unique id of the application that will be used to launch a URL for this protocol"}
        };

        public static Dictionary<string, string> DtoPropertyDescription => _dtoPropertyDescription;

        public void Validate(List<string> errors)
        {
            if (string.IsNullOrEmpty(Protocol))
            {
                errors.Add("Protocol name must be configured");
            }
        }
    }
}