using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Sulu.UrlParser
{
    internal class DefaultSshUrlParser : ParserBase
    {
        public override IDictionary<string, string> Parse(string url)
        {
            url = StripProtocol(url);
            url = url.Trim('/');
            
            var host = url;
            var user = "";
            var port = "22";

            var userHostDelim = url.LastIndexOf('@');
            if (userHostDelim > 0)
            {
                user = url.Substring(0, userHostDelim);
                host = url.Substring(userHostDelim + 1);
            }
            var hostParts = host.Split(':');
            if(hostParts.Length > 1)
            {
                host = hostParts[0];
                port = hostParts[1];
            }
            return new Dictionary<string, string>
            {
                {"host", host },
                {"user", HttpUtility.UrlDecode(user) },
                {"port", port }
            };

        }
    }
}
