using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Xunit;
using scalus.UrlParser;
using scalus.Dto;
using scalus.Platform;
using static scalus.Dto.ParserConfigDefinitions;

namespace scalus.Test
{

    public class TestLaunchArgs
    {
        private string prot = "myprotocol";
        private string host = "thisismyhost";
        private string port = "333";
        private string path = "thisisapath";
        private string query = "a small query";
        private string fragment = "fragmented";
        private string vault ="myvaultaddress";
        private string token = "some token";
        private string targetuser = "enduser";
        private string targethost = "targethost";
        private string targetport = "333";
        private string fulluser;
        private string  url;
        private string filename = Path.GetTempPath() + "tmpfile.rdp";

        private IDictionary<Token,string> SetupData()
        {
            fulluser = $"vaultaddress={vault}@token={token}@{targetuser}@{targethost}";
            url = $"{prot}://{fulluser}@{host}";


            return new Dictionary<Token,string>()
            {
                { Token.Protocol, prot }, 
                { Token.User, fulluser },
                { Token.Host, host },
                { Token.Port, port },
                { Token.Path, path },
                { Token.Query, query },
                { Token.Fragment, fragment },
                { Token.Vault, vault },
                { Token.Token, token },
                { Token.TargetUser, targetuser },
                { Token.TargetHost, targethost },
                { Token.TargetPort, targetport },
                { Token.OriginalUrl, url },
                { Token.RelativeUrl, $"{fulluser}@{host}" },
                { Token.GeneratedFile, filename },
                { Token.TempPath, Path.GetTempPath() },
                { Token.Home, "homedir" }
            };
        }

        [Fact]
        public void Test1()
        {
            var parser = new DefaultSshUrlParser(new Dto.ParserConfig(), SetupData());
            var args = new List<string>()
            {
                $"%{Token.Protocol}%",
                $"%{Token.User}%", 
                $"%{Token.Host}%",
                $"%{Token.Port}%",
                $"%{Token.Path}%",
                $"%{Token.Query}%",
                $"%{Token.Fragment}%",
                $"%{Token.Vault}%",
                $"%{Token.Token}%",
                $"%{Token.TargetUser}%",
                $"%{Token.TargetHost}%",
                $"%{Token.TargetPort}%",
                $"%{Token.OriginalUrl}%",
                $"%{Token.RelativeUrl}%",

                $"%{Token.GeneratedFile}%",
                $"%{Token.TempPath}%",
                $"%{Token.Home}%"
            };
               
            
            var newargs = parser.ReplaceTokens(args);
            var one = newargs.GetEnumerator();
            Assert.True(one.MoveNext());
            Assert.Equal(prot, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(fulluser, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(host, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(port, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(path, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(query, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(fragment, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(vault, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(token, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(targetuser, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(targethost, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(targetport, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(url, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal($"{fulluser}@{host}" , one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(filename, one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal(Path.GetTempPath(), one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal("homedir", one.Current);
            Assert.False(one.MoveNext());

            args = new List<string>
            {
                "%protocol",
                "user",
                "host%",
                "%host%%",
                "%host%hide"

            };
            newargs = parser.ReplaceTokens(args);
            one = newargs.GetEnumerator();
            Assert.True(one.MoveNext());
            Assert.Equal("%protocol", one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal("user", one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal("host%", one.Current);
            Assert.True(one.MoveNext());
            Assert.Equal("thisismyhost%", one.Current);

            Assert.True(one.MoveNext());
            Assert.Equal("thisismyhosthide", one.Current);
            Assert.False(one.MoveNext());

        }

        [Fact]
        public void Test2()
        {
            var list = ProtocolHandlerFactory.GetSupportedParsers();
            Assert.True(list.Count >0);
            Assert.Contains("ssh", list);
            Assert.Contains("rdp", list);
            Assert.Contains("url", list);
            Assert.Contains("telnet", list);

        }

        [Fact]
        public void TestMacParsing()
        {
            var str = @"(
        {
        CFBundleURLName = ""com.oneidentity.scalus.macos"";
        CFBundleURLSchemes =         (
            ssh,
            rdp, 
            telnet
        );
        Another = ""sldkfjdk2"";
        Some = (
        One = ""two"";
        );
    }
)";
            var list = MacOSProtocolRegistrar.ParseList(str);
            Assert.True(list.Count > 0, $"first:{list[0]}, second:{list[1]}");
            Assert.Equal(3, list.Count);
            Assert.Equal("ssh", list[0]);
            Assert.Equal("rdp", list[1]);
            Assert.Equal("telnet", list[2]);

            var newstr = MacOSProtocolRegistrar.ConstructNewValue(list);

        }

    }
}
