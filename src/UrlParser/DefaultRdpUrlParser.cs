// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultRdpUrlParser.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.UrlParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Util;
    using Serilog;
    using static OneIdentity.Scalus.Dto.ParserConfigDefinitions;

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

        public const string RdpPatternValue = "\\S=[s|i]:\\S+";
        public const string FullAddressKey = "full address";
        public const string UsernameKey = "username";
        public const string RdpPasswordHashKey = "password 51";

        private readonly IDictionary<string, Tuple<bool, string>> msArgList1 = new Dictionary<string, Tuple<bool, string>>();

        private List<(string, Token)> rdpKeys = new List<(string, Token)>
        {
            { ("alternate shell", Token.AlternateShell) },
            { ("remoteapplicationname", Token.Remoteapplicationname) },
            { ("remoteapplicationprogram", Token.Remoteapplicationprogram) },
            { ("remoteapplicationcmdline", Token.Remoteapplicationcmdline) },
        };

        private Dictionary<string, string> defaultArgs = new Dictionary<string, string>();

        private Regex rdpPattern = new Regex("^[^:]+:\\/\\/(([^:=]+)(:|=).:([^&]*))");
        private Regex rdpPatt = new Regex("&");

        public DefaultRdpUrlParser(ParserConfig config)
            : base(config)
        {
            FileExtension = ".rdp";
            GetDefaults();
        }

        public void GetDefaults()
        {
            var str = GetResource("Default.rdp");
            var list = str?.Split(Environment.NewLine);

            defaultArgs = ParseTemplate(list);
            return;
        }

        public override IDictionary<Token, string> Parse(string url)
        {
            Dictionary = DefaultDictionary();
            Dictionary[Token.OriginalUrl] = url;
            Dictionary[Token.Protocol] = Protocol(url) ?? "rdp";
            Dictionary[Token.RelativeUrl] = StripProtocol(url).TrimEnd('/');
            Dictionary[Token.Port] = "3389";

            var match = rdpPattern.Match(url.TrimEnd('/'));
            if (!match.Success)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result))
                {
                    throw new ParserException($"The RDP parser cannot parse the URL:{url}");
                }

                Log.Information($"Parsing URL{url} as a default URL");
                foreach (var (key, value) in defaultArgs)
                {
                    if (key.Equals(FullAddressKey, StringComparison.Ordinal))
                    {
                        msArgList1[key] = Tuple.Create(true, ":s:" + result.GetComponents(UriComponents.Host, UriFormat.Unescaped));
                    }
                    else if (key.Equals(UsernameKey, StringComparison.Ordinal))
                    {
                        msArgList1[key] = Tuple.Create(true, ":s:" + result.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped));
                    }
                    else
                    {
                        msArgList1[key] = Tuple.Create(true, value);
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

        public static string RemoveSpecialCharacters(string source)
        {
            if (source == null)
            {
                return source;
            }

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
            var newline = base.ReplaceTokens(line);
            var re = new Regex("(([^:]+):([^:]+):(.*))");
            var match = re.Match(newline);

            if (match.Success && !string.IsNullOrEmpty(match.Groups[2].Value) &&
                !string.IsNullOrEmpty(match.Groups[3].Value) &&
                string.IsNullOrEmpty(match.Groups[4].Value))
            {
                var name = match.Groups[2].Value;
                var val = match.Groups[3].Value + ":" + match.Groups[4].Value;
                if (msArgList1.ContainsKey(name) && msArgList1[name].Item1)
                {
                    val = msArgList1[name].Item2;
                    newline = name + ":" + val;
                }
            }

            return newline;
        }

        protected override IEnumerable<string> GetDefaultTemplate()
        {
            var list = new List<string>();
            foreach (var one in msArgList1)
            {
                list.Add(one.Key + ":" + one.Value.Item2);
            }

            return list;
        }

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

        private static Dictionary<string, string> ParseTemplate(IEnumerable<string> list)
        {
            var dict = new Dictionary<string, string>();
            if (list == null || !list.Any())
            {
                return dict;
            }

            foreach (var one in list)
            {
                var line = one.Split(":");
                if (line.Length < 3)
                {
                    continue;
                }

                dict[line[0]] = line[1] + ":" + line[2];
            }

            return dict;
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
                if (name.Equals(UsernameKey, StringComparison.Ordinal))
                {
                    if (value.Contains("%25") || value.Contains("%5c") || value.Contains("%20"))
                    {
                        value = HttpUtility.UrlDecode(value);
                    }
                    else
                    {
                        value = value.Replace("%3a", ":");
                    }

                    //Workaround a bug where 2 slashes were added to the connection URI instead of just 1
                    value = value.Replace("\\\\", "\\");

                    Dictionary[Token.User] = Regex.Replace(value, "^.:", string.Empty);
                    GetSafeguardUserValue(Dictionary);
                }
                else if (Regex.IsMatch(name, FullAddressKey))
                {
                    var hostval = value;

                    (string host, string port) = ParseHost(Regex.Replace(hostval, "^.:", string.Empty));
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

                    foreach (var one in rdpKeys)
                    {
                        if (Regex.IsMatch(name, one.Item1))
                        {
                            Dictionary[one.Item2] = value;
                            break;
                        }
                    }
                }

                msArgList1[name] = Tuple.Create(true, type + ":" + value);
            }

            foreach (var arg in defaultArgs)
            {
                if (!msArgList1.ContainsKey(arg.Key))
                {
                    msArgList1[arg.Key] = Tuple.Create(false, arg.Value);
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //Add hashed password so that the user isn't prompted to enter a password
                var passwordHash = GenerateRdpPasswordHash();
                msArgList1[RdpPasswordHashKey] = Tuple.Create(false, "b:" + passwordHash);
            }
        }

        [SupportedOSPlatform("windows")]
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
                Log.Warning($"Could not generate RDP password hash: {ex}");
            }

            return string.Empty;
        }
    }
}
