using System;
using System.Collections.Generic;

namespace scalus.Dto
{
    public class ProtocolMapping
    {
        public string Protocol { get; set; }
        public string AppId { get; set; }

        private static readonly Dictionary<string, string> _dtoPropertyDescription = new Dictionary<string, string>
        {
            {"Protocol", "The protocol string that can be launched by scalus"},
            {"AppId", "The unique id of the application that will be used to launch a URL for this protocol"}
        };

        public static Dictionary<string, string> DtoPropertyDescription => _dtoPropertyDescription;

        public void Validate()
        {
            Protocol.NotNullValue(nameof(Protocol));
        }
    }
}