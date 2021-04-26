using scalus.Dto;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using static scalus.Dto.ParserConfigDefinitions;

namespace scalus.UrlParser
{
    [ParserName("rdp")]  
    internal class DefaultRdpUrlParser : BaseParser
    {
        //This class parses an RDP string of the form
        // <protocol>://<expression>[&<expression>]...
        // where :
        //      protocol    :  rdp
        //      expression  :  <name>=<type>:<value>
        //      name        :  any valid MS RDP setting, e.g. 'full address', 'username'
        //                     Name and value strings can be url encoded
        //      type        :  i|s

        //  query values for:    
        //  full address    :  <ipaddress>[:<port>]
        //  username        :  <username>|<safeguardauth>
        //  safeguardauth   :  vaultaddress(=|~)<ipaddress>(%|@)token
        //                     Name and value strings can be url encoded

        //If not in this format, it will default to parsing the string as a standard URL
        public Regex RdpPattern = new Regex("(([^:]+)://)?((([^&=]+)=([^&]+))(&(([^&=]+)=([^&]+)))*)");
        public Regex RdpPatt = new Regex("&");
        private List<string> _msArgList { get; set; } = new List<string>();

        public const string rdpPattern = "\\S=[s|i]:\\S+";
        public const string username = "username";
        public const string fulladdress = "full address";
        public DefaultRdpUrlParser(ParserConfig config) : base(config)
        {            
            FileExtension = ".rdp";
        }
        public DefaultRdpUrlParser(ParserConfig config, IDictionary<Token, string> dictionary=null, List<string> defs = null) : this(config)
        {
            if (dictionary != null)
            {
                Dictionary = dictionary;
            }
            if (defs != null)
            {
                _msArgList = defs;
            }           
        }
        
        public override  IDictionary<Token,string> Parse(string url)
        {
            Dictionary = new Dictionary<Token, string>();
            Dictionary[Token.OriginalUrl] = url; 
            Dictionary[Token.Protocol] = Protocol(url)??"rdp";
            Dictionary[Token.RelativeUrl] = StripProtocol(url).TrimEnd('/');            
            Dictionary[Token.Port] = "3389";
            
            var match = RdpPattern.Match(url.TrimEnd('/'));
            if (!match.Success)
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri result))
                {
                    Parse(result);
                    return Dictionary;
                }
            }
            else
            {
                ParseArgs(match.Groups[3].Value);
            }

            ParseConfig();
            return Dictionary;           
        }
        protected override IEnumerable<string> GetDefaultTemplate()
        {
            return _msArgList;
        }

        public static readonly Dictionary<string, string> defaultArgs = new Dictionary<string, string>()
        {
            {"full address", ":s:%Host%"},
            {"username", ":s:%user%"},
            {"screen mode id", ":i:2"},
            {"use multimon", ":i:0"},
            {"desktopwidth", ":i:1024"},
            {"desktopheight", ":i:768"},
          //  {"session bpp", ":i:16"},
          //  {"winposstr", ":s:0,3,0,0,1024,768"},
            {"compression", ":i:1"},
            {"keyboardhook", ":i:2"},
            {"audiocapturemode", ":i:0"},
            {"videoplaybackmode", ":i:1"},
           // {"connection type", ":i:7"},
            {"networkautodetect", ":i:1"},
            {"bandwidthautodetect", ":i:1"},
            //{"displayconnectionbar", ":i:1"},
            //{"enableworkspacereconnect", ":i:0"},
            //{"disable wallpaper", ":i:1"},
            //{"allow font smoothing", ":i:1"},
            //{"allow desktop composition", ":i:1"},
            //{"disable full window drag", ":i:1"},
            //{"disable menu anims", ":i:1"},
            //{"disable themes", ":i:0"},
            //{"disable cursor setting", ":i:0"},
            //{"bitmapcachepersistenable", ":i:1"},
            {"audiomode", ":i:0"},
            {"redirectprinters", ":i:1"},
            {"redirectcomports", ":i:0"},
            {"redirectsmartcards", ":i:1"},
            {"redirectclipboard", ":i:1"},
            //{"redirectposdevices", ":i:0"},
            {"autoreconnection enabled", ":i:1"},
            {"authentication level", ":i:2"},
            //{"prompt for credentials", ":i:0"},
            //{"prompt for credentials on client", ":i:0"},
            //{"negotiate security layer", ":i:1"},
            {"remoteapplicationmode", ":i:0"},
            {"alternate shell", ":s:"},
            //{"shell working directory", ":s:"},
            {"gatewayhostname", ":s:"},
            {"gatewayusagemethod", ":i:4"},
            {"gatewaycredentialssource", ":i:4"},
            {"gatewayprofileusagemethod", ":i:0"},
            {"promptcredentialonce", ":i:0"},
            //{"gatewaybrokeringtype", ":i:0"},
            //{"use redirection server name", ":i:0"},
            //{"rdgiskdcproxy", ":i:0"},
            //{"kdcproxyname", ":s:"},

            {"alternate full address", "s:"},
            {"domain", "s:"},
            {"enablecredsspsupport", ":i:0"},
            {"disableconnectionsharing", ":i:0"},
            {"encode redirected video capture", ":i:1"},
            {"redirected video capture encoding quality", ":i:0"},
            {"camerastoredirect", "s:"},
            {"devicestoredirect", ":s:"},
            {"drivestoredirect", ":s:"},
            {"usbdevicestoredirect", ":s:"},
            {"selectedmonitors", ":s:"},
            {"maximizetocurrentdisplays", ":i:0"},
            {"singlemoninwindowedmode", ":i:0"},
            {"smart sizing", ":i:0"},
            {"dynamic resolution", ":i:1"},
            {"desktop size id", ":i:1"},
            {"desktopscalefactor", ":i:100"},
            {"remoteapplicationexpandcmdline", ":i:1"},
            {"remoteapplicationexpandworkingdir", ":i:1"},
            {"remoteapplicationicon", ":s:"},
            {"remoteapplicationname", "s:"},
            {"remoteapplicationprogram", "s:"},
        };

        private const string RdpPasswordHashKey = "password 51:b";

        private void ParseArgs(string clArgs)
        {
            var usedNames = new HashSet<string>();
            var re = new Regex("([^=]+)=(\\S+)");
            var args = clArgs.Split('&');
            foreach (var arg in args)
            {
                var m = re.Match(arg);
                if (!m.Success)
                {
                    continue;
                }
                var name = HttpUtility.UrlDecode(m.Groups[1].Value);
                var value = m.Groups[2].Value;
                if (name.Equals(username))
                {
                    if (value.IndexOf("%25") >= 0 || value.IndexOf("%5c")>= 0)
                    {
                        value = HttpUtility.UrlDecode(value);
                    }
                    else
                    {
                        value = value.Replace("%3a", ":");
                    }

                    //Workaround a bug where 2 slashes were added to the connection URI instead of just 1
                    value = value.Replace("\\\\", "\\");
                    
                    Dictionary[Token.User] = Regex.Replace(value, "^.:", "");
                    GetSafeguardUserValue();
                }
                else if (Regex.IsMatch(name, fulladdress))
                {
                    value = HttpUtility.UrlDecode(value);
                    var hostval = m.Groups[2].Value;

                    (string host, string port) = ParseHost(Regex.Replace(hostval, "^.:", ""));
                    Dictionary[Token.Host] = host;
                    
                    if (!string.IsNullOrEmpty(port))
                    {
                        Dictionary[Token.Port] = port;
                    }
                    else
                    {
                        Dictionary[Token.Port] = "3389";
                    }
                }
                else
                {
                    value = HttpUtility.UrlDecode(value);
                }

                _msArgList.Add($"{name}:{value}");
                usedNames.Add(name);
            }

            foreach (var arg in defaultArgs)
            {
                if (!usedNames.Contains(arg.Key))
                {
                    _msArgList.Add($"{arg.Key}:{arg.Value}");
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //Add hashed password so that the user isn't prompted to enter a password
                var passwordHash = GenerateRdpPasswordHash();
                _msArgList.Add(RdpPasswordHashKey + ":" + passwordHash);
            }
        }
    
        private static string GenerateRdpPasswordHash()
        {
            try
            {
                var byteArray = Encoding.UTF8.GetBytes("sg");
                var cypherData = ProtectedData.Protect(byteArray, null, DataProtectionScope.CurrentUser);
                var hex = new StringBuilder(cypherData.Length * 2);
                foreach (var b in cypherData)
                {
                    hex.AppendFormat("{0:x2}", b);
                }
                return hex.ToString();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Could not generate RDP password hash: {ex}");
            }

            return "";
        }

        public static string RemoveSpecialCharacters(string source)
        {
            if (source == null) return source;

            var sb = new StringBuilder();
            foreach (char ch in source)
            {
                if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '.' || ch == '_')
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }
}
