using System;
using System.Collections.Generic;
using System.Linq;

namespace scalus.Dto
{
    public static class Extensions
    {
        public static Dictionary<K, V> Append<K, V>(this Dictionary<K, V> first, Dictionary<K, V> second)
        {
            var res = new Dictionary<K, V>(first);
            List<KeyValuePair<K, V>> pairs = second.ToList();
            pairs.ForEach(pair => res[pair.Key] =pair.Value);
            return res;
        }

        
        public static void NotNullValue(this string val, string name)
        {
            if (string.IsNullOrEmpty(val))
            {
                throw new Exception($"Application property:{name} must contain a value");
            }
        }

    }

    public class ScalusConfig
    {
        public List<ProtocolMapping> Protocols { get; set; }
        public List<ApplicationConfig> Applications { get; set; }

        private static Dictionary<string, string> _dtoPropertyDescription = new Dictionary<string, string>
        {
            {"Protocols","The list of protocols configured for scalus"},
            {"Applications","The list of applications available to use"}
        };
        public static Dictionary<string, string> DtoPropertyDescription => _dtoPropertyDescription.Append(ProtocolMapping.DtoPropertyDescription).Append(ApplicationConfig.DtoPropertyDescription);

        public void Validate()
        {
            foreach (var one in Protocols)
            {
                one.Validate();
                if (string.IsNullOrEmpty(one.AppId))
                    continue;
                var found = Applications.Any(app => app.Id == one.AppId);

                if (!found)
                {
                    throw new Exception($"Protocol:{one} is mapped to unknown application id:{one.AppId}");
                }
            }

            var list = Protocols.Select(one => one.Protocol).ToList();
            if (Protocols.Count != list.Distinct().Count())
            {
                throw new Exception($"Protocols list must not contain multiple definitions for a protocol string");
            }

            foreach (var one in Applications)
            {
                one.Validate();
            }

        }
    }
}
