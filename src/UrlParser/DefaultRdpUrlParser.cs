using scalus.Dto;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Serilog;
using static scalus.Dto.ParserConfigDefinitions;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using System.IO;

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
        //  safeguardauth   :  vaultaddress(=|~)<ipaddress>(%|@)token~<token>[account~<name>%asset~<name>]
        //                     Name and value strings can be url encoded

        //If not in this format, it will default to parsing the string as a standard URL
        public Regex RdpPattern = new Regex("^[^:]+:\\/\\/(([^:=]+)(:|=).:([^&]*))");
        public Regex RdpPatt = new Regex("&");

        private readonly IDictionary<string, Tuple<bool, string>> _msArgList1 = new Dictionary<string, Tuple<bool, string>>();
       
        private static string GetResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            var resourceName = assembly.GetManifestResourceNames().Single(x => x.EndsWith(name, StringComparison.OrdinalIgnoreCase));
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                Debug.Assert(stream != null, "stream != null");

                using (var reader = new StreamReader(stream, Encoding.ASCII))
                {
                    return reader.ReadToEnd();
                }
            }
        }


        
        public const string rdpPattern = "\\S=[s|i]:\\S+";
        public const string FullAddressKey = "full address";
        public const string UsernameKey = "username";
        public const string RdpPasswordHashKey = "password 51";
        public const string AlternateShellKey = "alternate shell";
        public const string RemoteapplicationnameKey = "remoteapplicationname";
        public const string RemoteapplicationprogramKey = "remoteapplicationprogram";

       
        public Dictionary<string, string> DefaultArgs = new Dictionary<string, string>();

       
        private Dictionary<string,string> ParseTemplate(IEnumerable<string> list)
        {
            var dict = new Dictionary<string, string>();
            if (list == null || list.Count()== 0)
                return dict;
            foreach (var one in list)
            {                
                var line = one.Split(":");
                if (line.Length < 3)
                    continue;
                dict[line[0]] = line[1] + ":" + line[2];
            }
            return dict;
        }

        public void GetDefaults()
        {
            var str = GetResource("Default.rdp");
            var list =str?.Split(Environment.NewLine);

            DefaultArgs = ParseTemplate(list);
            return;
        }

        public DefaultRdpUrlParser(ParserConfig config) : base(config)
        {            
            FileExtension = ".rdp";
            GetDefaults();
            
        }

        public override  IDictionary<Token,string> Parse(string url)
        {
            Dictionary = DefaultDictionary();
            Dictionary[Token.OriginalUrl] = url; 
            Dictionary[Token.Protocol] = Protocol(url)??"rdp";
            Dictionary[Token.RelativeUrl] = StripProtocol(url).TrimEnd('/');            
            Dictionary[Token.Port] = "3389";
            
            var match = RdpPattern.Match(url.TrimEnd('/'));
            if (!match.Success)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result))
                    throw new Exception($"The RDP parser cannot parse the URL:{url}");
                Log.Information($"Parsing URL{url} as a default URL");
                foreach (var (key, value) in DefaultArgs)
                {
                    if (key.Equals(FullAddressKey))
                    {
                        _msArgList1[key] = Tuple.Create(true, ":s:"+ result.GetComponents(UriComponents.Host, UriFormat.Unescaped));

                    }
                    else if (key.Equals(UsernameKey))
                    {
                        _msArgList1[key] = Tuple.Create(true, ":s:"+ result.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped));
                    }
                    else {
                        _msArgList1[key] = Tuple.Create(true, value);
                    }
                }
                Parse(result);
            }
            else
            {
                Log.Information($"Parsing URL{url} as an rdp URL");
                ParseArgs(Dictionary[Token.RelativeUrl]);
                ParseConfig();
            }
            //tokens required are username and host

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
            var list = new List<string>();
            foreach(var one in _msArgList1)
            {
                list.Add(one.Key + ":" + one.Value.Item2);
            }
            return list;
        }
        
        private void ParseArgs(string clArgs)
        {
            var re = new Regex("(([^=:]+)[=|:](s|i):(.*))");
            var args = clArgs.Split('&');
            foreach (var arg in args)
            {
                var m = re.Match(arg);
                if (!m.Success)
                {
                    continue;
                }
                string name;
                string type;
                string value;
                name = m.Groups[2].Value;
                type = m.Groups[3].Value;
                value = m.Groups[4].Value;

                name = HttpUtility.UrlDecode(name);
                value = HttpUtility.UrlDecode(value);
                if (name.Equals(UsernameKey))
                {
                    if ((value.IndexOf("%25", StringComparison.Ordinal) >= 0) || 
                        (value.IndexOf("%5c", StringComparison.Ordinal)>= 0) ||
                            (value.IndexOf("%20", StringComparison.Ordinal) >=0))
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
                    GetSafeguardUserValue(Dictionary);
                }
                else if (Regex.IsMatch(name, FullAddressKey))
                {
                    var hostval = value;

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
                if (Regex.IsMatch(name, AlternateShellKey))
                {
                    Dictionary[Token.AlternateShell] = value;
                }
                else if (Regex.IsMatch(name, RemoteapplicationnameKey))
                {
                    Dictionary[Token.Remoteapplicationname] = value;
                }
                else if (Regex.IsMatch(name, RemoteapplicationprogramKey))
                {
                    Dictionary[Token.Remoteapplicationprogram] = value;
                }
                _msArgList1[name] = Tuple.Create(true, type + ":" + value);
            }
             

            foreach (var arg in DefaultArgs)
            {
                if (!_msArgList1.ContainsKey(arg.Key))
                {
                    _msArgList1[arg.Key] = Tuple.Create(false, arg.Value);
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //Add hashed password so that the user isn't prompted to enter a password
                var passwordHash = GenerateRdpPasswordHash();
                _msArgList1[RdpPasswordHashKey] = Tuple.Create(false, "b:" + passwordHash);
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
                Log.Warning( $"Could not generate RDP password hash: {ex}");
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

        public override string ReplaceTokens(string line)
        {
            var newline = line;
            foreach (var variable in Dictionary)
            {
                // TODO: Make this more robust. Edge case escapes don't work.
                newline = Regex.Replace(newline, $"%{variable.Key}%", variable.Value ?? string.Empty, RegexOptions.IgnoreCase);
            }
            var re = new Regex("(([^:]+):([^:]+):(.*))");
            var match = re.Match(newline);

            if (match.Success && !string.IsNullOrEmpty(match.Groups[2].Value) &&
                !string.IsNullOrEmpty(match.Groups[3].Value) &&
                string.IsNullOrEmpty(match.Groups[4].Value))
            {
                var name = match.Groups[2].Value;
                var val = match.Groups[3].Value + ":" + match.Groups[4].Value;
                if (_msArgList1.ContainsKey(name) && _msArgList1[name].Item1)
                {
                    val = _msArgList1[name].Item2;
                    newline = name + ":" + val;
                }
            }
            return newline;         
        }
    }
}
