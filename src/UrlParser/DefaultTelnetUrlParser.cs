using scalus.Dto;
using System.Collections.Generic;
using static scalus.Dto.ParserConfigDefinitions;

namespace scalus.UrlParser
{
    [ParserName("telnet")]  
    internal class DefaultTelnetUrlParser : DefaultSshUrlParser
    {
        //Identical to the ssh parser
        public DefaultTelnetUrlParser(ParserConfig config) : base(config) {
            FileExtension = ".telnet";
        }
        public DefaultTelnetUrlParser(ParserConfig config, IDictionary<Token, string> dictionary) : this(config) {
           if (dictionary != null)
                Dictionary = dictionary;
        }
    }
}
