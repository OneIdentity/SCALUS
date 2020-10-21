using System.Collections.Generic;

namespace Sulu.Dto
{
    public class ProtocolConfig
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Protocol { get; set; }
        public ParserConfig Parser { get; set; }
        public string Exec { get; set; }
        public List<string> Args { get; set; }
    }
}
