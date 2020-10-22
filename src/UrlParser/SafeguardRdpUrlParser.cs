using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Sulu.UrlParser
{
    class RdpFileUrlParser : ParserBase
    {
        public override IDictionary<string, string> Parse(string url)
        {
            var tempFile = Path.GetTempFileName() + ".rdp";
            Disposables.Add(Disposable.Create(() => File.Delete(tempFile)));
            url = StripProtocol(url);
            url = url.Trim('/');
            var args = ParseArgs(url);
            File.WriteAllText(tempFile, string.Join("\r\n", args));
            var result = new Dictionary<string, string>
            {
                { "rdpfile", tempFile }
            };
            return result;
        }

        public override bool WaitForProcessStartup => true;

        private static readonly Dictionary<string, string> defaultArgs = new Dictionary<string, string>()
        {
            {"screen mode id", ":i:2"},
            {"use multimon", ":i:0"},
            {"desktopwidth", ":i:1024"},
            {"desktopheight", ":i:768"},
            {"session bpp", ":i:16"},
            {"winposstr", ":s:0,3,0,0,1024,768"},
            {"compression", ":i:1"},
            {"keyboardhook", ":i:2"},
            {"audiocapturemode", ":i:0"},
            {"videoplaybackmode", ":i:1"},
            {"connection type", ":i:7"},
            {"networkautodetect", ":i:1"},
            {"bandwidthautodetect", ":i:1"},
            {"displayconnectionbar", ":i:1"},
            {"enableworkspacereconnect", ":i:0"},
            {"disable wallpaper", ":i:1"},
            {"allow font smoothing", ":i:1"},
            {"allow desktop composition", ":i:1"},
            {"disable full window drag", ":i:1"},
            {"disable menu anims", ":i:1"},
            {"disable themes", ":i:0"},
            {"disable cursor setting", ":i:0"},
            {"bitmapcachepersistenable", ":i:1"},
            {"audiomode", ":i:0"},
            {"redirectprinters", ":i:1"},
            {"redirectcomports", ":i:0"},
            {"redirectsmartcards", ":i:1"},
            {"redirectclipboard", ":i:1"},
            {"redirectposdevices", ":i:0"},
            {"autoreconnection enabled", ":i:1"},
            {"authentication level", ":i:2"},
            {"prompt for credentials", ":i:0"},
            {"prompt for credentials on client", ":i:0"},
            {"negotiate security layer", ":i:1"},
            {"remoteapplicationmode", ":i:0"},
            {"alternate shell", ":s:"},
            {"shell working directory", ":s:"},
            {"gatewayhostname", ":s:"},
            {"gatewayusagemethod", ":i:4"},
            {"gatewaycredentialssource", ":i:4"},
            {"gatewayprofileusagemethod", ":i:0"},
            {"promptcredentialonce", ":i:0"},
            {"gatewaybrokeringtype", ":i:0"},
            {"use redirection server name", ":i:0"},
            {"rdgiskdcproxy", ":i:0"},
            {"kdcproxyname", ":s:"}
        };
        
        private const string RdpPasswordHashKey = "password 51:b";

        private static List<string> ParseArgs(string clArgs)
        {
            var list = new List<string>();
            var usedNames = new HashSet<string>();
            var args = clArgs.Split('&');
            foreach (var arg in args)
            {
                var argParts = arg.Split('=');
                var name = HttpUtility.UrlDecode(argParts[0]);
                var value = argParts[1];
                if (name.Equals("username"))
                {
                    if (value.IndexOf("%25") >= 0)
                    {
                        value = HttpUtility.UrlDecode(value);
                    }
                    else
                    {
                        value = value.Replace("%3a", ":");
                    }

                    //Workaround a bug where 2 slashes were added to the connection URI instead of just 1
                    value = value.Replace("\\\\", "\\");

                    //Grab the target address off of the connection string
                    var index = value.LastIndexOf("%");
                    if (index >= 0)
                    {
                        var address = value.Substring(index + 1);

                        //Remove the port if it exists
                        index = address.LastIndexOf(":");
                        if (index >= 0)
                        {
                            address = address.Substring(0, index);
                        }

                        //Note: mstsc only displays the filename in the title up to the first period. So we'll change any periods
                        //in the address to underscores and add a period after to hide the ugly part of the filename.
                        //targetAddress = RemoveSpecialCharacters(address).Replace(".", "_") + ".";
                    }
                }
                else
                {
                    value = HttpUtility.UrlDecode(value);
                }

                list.Add($"{name}:{value}");
                usedNames.Add(name);
            }

            foreach (var arg in defaultArgs)
            {
                if (!usedNames.Contains(arg.Key))
                {
                    list.Add($"{arg.Key}:{arg.Value}");
                }
            }

            //Add hashed password so that the user isn't prompted to enter a password
            var passwordHash = GenerateRdpPasswordHash();
            list.Add(RdpPasswordHashKey + ":" + passwordHash);

            return list;
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
