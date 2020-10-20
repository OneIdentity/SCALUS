using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu.Dto
{
    public class ParserConfig
    {
        public string Type { get; set; }
        public string Id { get; set; }
    }

    public class ProtocolConfig
    {
        public string Name { get; set; }
        public ParserConfig Parser { get; set; }
        public string Exec { get; set; }
        public string Args { get; set; }
    }

    public class SuluConfig
    {
        public List<ProtocolConfig> Protocols { get; set; }
    }
}
