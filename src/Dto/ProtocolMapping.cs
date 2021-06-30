using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace scalus.Dto
{
    public class ProtocolMapping
    {
        public static bool ValidateProtocol(string val, out string err)
        {
            err=null;
            if (string.IsNullOrEmpty(val))
            {
                err =("Protocol name must be configured");
                return false;
            }
            if (Regex.IsMatch(val, "^[a-zA-Z][a-zA-Z0-9-+.]+$"))
            {
                return true;
            }
            err =($"Protocol name contains invalid chars:{val}");
            return false;
        }

        [JsonRequired]
        public string Protocol { get;set; }
        public string AppId { get; set; }

        private static readonly Dictionary<string, string> _dtoPropertyDescription = new Dictionary<string, string>
        {
            {nameof(Protocol), "The protocol string that will be registered for scalus"},
            {nameof(AppId), "The unique id of the application that will be used to launch a URL for this protocol"}
        };

        public static Dictionary<string, string> DtoPropertyDescription => _dtoPropertyDescription;

        public void Validate(List<string> errors)
        {
            if (!ValidateProtocol(Protocol, out string err))
            {
                errors.Add(err);
            }
        }
    }
}