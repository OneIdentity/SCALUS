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
            {"Protocols","The list of protocols configured for scalus"},
            {"Applications","The list of applications available to use"}
        };
        public static Dictionary<string, string> DtoPropertyDescription => _dtoPropertyDescription.Append(ProtocolMapping.DtoPropertyDescription).Append(ApplicationConfig.DtoPropertyDescription);
    }
}
