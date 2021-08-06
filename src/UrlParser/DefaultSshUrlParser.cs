using scalus.Dto;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Serilog;
using static scalus.Dto.ParserConfigDefinitions;

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

        public Regex ScpPattern = new Regex("(([^:]+)://)?((.+)@([^:]+)(:(\\d+))?)", RegexOptions.IgnoreCase);
        
        public override IDictionary<Token, string> Parse(string url)
        {
            Dictionary = DefaultDictionary();
            Dictionary[Token.OriginalUrl] = url;
            Dictionary[Token.Protocol] = Protocol(url)??"ssh";
            Dictionary[Token.RelativeUrl] = StripProtocol(url);
            url = url.TrimEnd('/');
            var match = ScpPattern.Match(url);
            if (!match.Success)
            {
                if (url.Contains("%40"))
                {
                    var decoded = HttpUtility.UrlDecode(url);
                    match = ScpPattern.Match(decoded);
                }
            }
            if (!match.Success)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result))
                {
                    throw new Exception($"The SSH parser cannot parse URL:{url}");
                }
                Parse(result);
            }
            else
            {
                SetValue(match, 2, Token.Protocol, false, "ssh");
                SetValue(match, 4, Token.User, true);
                SetValue(match, 5, Token.Host, false);
                SetValue(match, 7, Token.Port, false, "22");

                GetSafeguardUserValue();
                ParseConfig();
            }

            if (!Dictionary.ContainsKey(Token.User) || string.IsNullOrEmpty(Dictionary[Token.User]))
            {
                Log.Warning($"The RDP parser could not extract the '{Token.User}' token from the url:{url}");
            }
            if (!Dictionary.ContainsKey(Token.Host) || string.IsNullOrEmpty(Dictionary[Token.Host]))
            {
                Log.Warning($"The RDP parser could not extract the '{Token.Host}' token from the url:{url}");
            }
            return Dictionary;        
        }

        protected override IEnumerable<string> GetDefaultTemplate()
        {
            Serilog.Log.Error("No default template is defined in this parser");
            throw new NotImplementedException();
        }
    }
}
