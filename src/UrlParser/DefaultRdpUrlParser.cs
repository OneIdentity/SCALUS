using Sulu.Dto;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace Sulu.UrlParser
{
    class DefaultRdpUrlParser : ParserBase
    {
        public DefaultRdpUrlParser(ParserConfig config) : base(config) { }

        Regex SafeguardUrlRegex = new Regex(@"rdp://full\+address.+username.+token.+:\d+");
        // rdp://full+address=s:10.5.32.168:3389&username=s:localhost%5cvaultaddress%7e10.5.33.238%25token%7epdFwRQSofwL6xJV4Ud32g4TXKM7XgXkYU8ks4i5GQHURRoBiFq5Rjr4dT%25win10-acct1%2510.5.60.94:3389
        // $host = 10.5.32.168
        // $port = 3389
        // $user = localhost\vaultaddress~10.5.33.238%token~pdFwRQSofwL6xJV4Ud32g4TXKM7XgXkYU8ks4i5GQHURRoBiFq5Rjr4dT%win10-acct1%10.5.60.94:3389
        public override IDictionary<string, string> Parse(string url)
        {
            if(SafeguardUrlRegex.IsMatch(url))
            {
                return ParseSafeguardRdpUrl(url);
            }
            Uri result;
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out result))
            {
                return ParseStandardUrl(result);
            }
            Serilog.Log.Warning($"sulu doesn't know how to parse rdp url: {url}");
            return null;
        }

        IDictionary<string, string> ParseStandardUrl(Uri url)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("host", url.GetComponents(UriComponents.Host, UriFormat.Unescaped));
            result.Add("user", url.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped));
            result.Add("port", url.GetComponents(UriComponents.Port, UriFormat.Unescaped));
            return result;
        }

        IDictionary<string,string> ParseSafeguardRdpUrl(string url)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            url = StripProtocol(url);
            url = url.Trim('/');
            var parts = url.Split('&');
            var address = parts[0];
            var username = parts[1];
            address = address.Replace("full+address=s:", "");
            var addressParts = address.Split(':');
            result.Add("host", addressParts[0]);
            result.Add("port", addressParts[1]);
            username = username.Replace("username=s:", "");
            username = HttpUtility.UrlDecode(username);
            result.Add("user", username);
            return result;
        }
    }
}
