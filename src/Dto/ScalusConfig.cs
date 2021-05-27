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
    }

    public class ScalusConfig
    {
        public List<ProtocolMapping> Protocols { get; set; }
        public List<ApplicationConfig> Applications { get; set; }

        private static Dictionary<string, string> _dtoPropertyDescription = new Dictionary<string, string>
        {
            {nameof(Protocols),"The list of protocols configured for scalus"},
            {nameof(Applications),"The list of applications available to use"}
        };
        public static Dictionary<string, string> DtoPropertyDescription => _dtoPropertyDescription.Append(ProtocolMapping.DtoPropertyDescription).Append(ApplicationConfig.DtoPropertyDescription);

        public void Validate(List<string> errors)
        {
            if (Protocols != null)
            {
                foreach (var one in Protocols)
                {
                    one.Validate(errors);
                    if (string.IsNullOrEmpty(one.AppId))
                        continue;
                    if (string.IsNullOrEmpty(one.Protocol))
                        continue;
                    var foundapp = Applications?.FirstOrDefault(a => 
                        !string.IsNullOrEmpty(a.Id) && a.Id.Equals(one.AppId, StringComparison.InvariantCultureIgnoreCase));

                    if (foundapp == null)
                    {
                        errors.Add($"Protocol:{one.Protocol} is mapped to undefined application id:{one.AppId}");
                    }

                    else if (string.IsNullOrEmpty(foundapp.Protocol))
                    {
                        errors.Add($"Protocol:{one.Protocol} is mapped to application:{foundapp.Name}({one.AppId}) which has no protocol defined");
                    }
                    else if (!foundapp.Protocol.Equals(one.Protocol, StringComparison.InvariantCultureIgnoreCase))
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

        }
    }
}
