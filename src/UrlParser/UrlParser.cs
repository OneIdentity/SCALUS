using scalus.Dto;
using System;
using System.Collections.Generic;
using static scalus.Dto.ParserConfigDefinitions;

namespace scalus.UrlParser
{
    [ParserName("url")]  
    internal class UrlParser : BaseParser
    {
        //This class parses a standard URL into components:
  
        public UrlParser( ParserConfig config): base(config){ 
            FileExtension = "url";
        }
        public UrlParser( ParserConfig config, IDictionary<Token, string> dictionary): this(config){ 
            if (dictionary != null)
                Dictionary = dictionary;
        }
        public override IDictionary<Token, string> Parse(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri result))
            {
                Parse(result);
                return Dictionary;
            }
            Serilog.Log.Warning($"The string does not appear to be a valid URL: {url}");
            Dictionary[Token.OriginalUrl] = url;
            Dictionary[Token.Protocol] = Protocol(url);
            Dictionary[Token.RelativeUrl] = StripProtocol(url);
            ParseConfig();            
            return Dictionary;
        }    
        protected override IEnumerable<string> GetDefaultTemplate()
        {
            Serilog.Log.Error("No default template is supported for this parser type");
            throw new Exception("No default file is defined for this parser type");
        }

    }
}
