using scalus.Dto;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace scalus.UrlParser
{
    [ParserName("ssh")]  
    internal class DefaultSshUrlParser : BaseParser
    {
        //This class parses a string in the format:
        //  - <protocol>://<user>@<host>[:<port>]
        //  where:
        //      protocol: ssh
        //      user    : Username - this can contain a Safeguard auth string
        //If this doesnt match, it defaults to parsing the string as a standard URL.
        public DefaultSshUrlParser(ParserConfig config) : base(config) {
            FileExtension = ".ssh";
        }
        public DefaultSshUrlParser(ParserConfig config, IDictionary<Token, string> dictionary) : this(config) {
           if (dictionary != null)
                Dictionary = dictionary;
        }

        public Regex ScpPattern = new Regex("(([^:]+)://)?((\\S+)@([^:]+)(:(\\d+))?)", RegexOptions.IgnoreCase);
        
        public override IDictionary<Token, string> Parse(string url)
        {
            Dictionary[Token.OriginalUrl] = url;
            Dictionary[Token.Protocol] = Protocol(url);
            Dictionary[Token.RelativeUrl] = StripProtocol(url);
            url = url.TrimEnd('/');
            var match = ScpPattern.Match(url);
            if (!match.Success)
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri result))
                {
                    Parse(result);
                    return Dictionary;
                }
                return Dictionary;
            }
            SetValue(match, 2, Token.Protocol, false, "ssh");
            SetValue(match, 3, Token.RelativeUrl , false);
            SetValue(match, 4, Token.User , true);
            SetValue(match, 5, Token.Host, false );
            SetValue(match, 7, Token.Port, false, "22");

            GetSafeguardUserValue();
            ParseConfig();
            return Dictionary;        
        }

        protected override IEnumerable<string> GetDefaultTemplate()
        {
            Serilog.Log.Error("No default template is defined in this parser");
            throw new NotImplementedException();
        }
    }
}
