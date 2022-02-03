using System;
using scalus.Dto;
using scalus.UrlParser;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using static scalus.Dto.ParserConfigDefinitions;
using System.Runtime.InteropServices;

namespace scalus.Test
{
    public class TestDefaultRdpUrlParser
    {

        [Fact]
        public void Test1()
        {
            //Rdp string
            //full+address=s:host&username=s:safeguarduserstring
            using (var sut = new DefaultRdpUrlParser(new Dto.ParserConfig()))
            {
                var prot = "rdp";
                var host = "10.5.32.168";
                var port = "3389";
                var vault = "10.5.33.238";
                var token = "epdFwRQSofwL6xJV4Ud32g4TXKM7XgXkYU8ks4i5GQHURRoBiFq5Rjr4dT";
                var targetuser = "win10-acct1%20with%20Space";
                var decodedTargetUser = "win10-acct1 with Space";
                var targethost = "10.5.60.94";
                var targetport = "3389";
                var user = $"username=s:localhost%5cvaultaddress%7e{vault}%25token%7e{token}%25{targetuser}%25{targethost}:{targetport}";
                var decodedUser = $"localhost\\vaultaddress~{vault}%token~{token}%{decodedTargetUser}%{targethost}:{targetport}";
                var str = $"full+address=s:{host}:{port}&{user}";
                var dictionary = sut.Parse($"{str}/");
                Assert.Equal($"{str}/", dictionary[Token.OriginalUrl]);
                Assert.Equal(str, dictionary[Token.RelativeUrl]);
                Assert.Equal(prot, dictionary[Token.Protocol]);
                Assert.Equal(host, dictionary[Token.Host]);
                Assert.Equal(port, dictionary[Token.Port]);
                Assert.Equal(decodedUser, dictionary[Token.User]);
                Assert.Equal(vault, dictionary[Token.Vault]);
                Assert.Equal(token, dictionary[Token.Token]);
                Assert.Equal(decodedTargetUser, dictionary[Token.TargetUser]);
                Assert.Equal(targethost, dictionary[Token.TargetHost]);
                Assert.Equal(targetport, dictionary[Token.TargetPort]);
                Assert.Equal(string.Empty, dictionary[Token.GeneratedFile]);

                //Rdp string
                //rdp://full address=s:host&username=s:user
                var uri = $"{prot}://{str}/";
                dictionary = sut.Parse($"{uri}");
                Assert.Equal(uri, dictionary[Token.OriginalUrl]);
                Assert.Equal(str, dictionary[Token.RelativeUrl]);
                Assert.Equal(prot, dictionary[Token.Protocol]);
            }
        }

        private bool check(string line, string name, string val, List<string> names, ref int count, string sep = ":")
        {
            if (Regex.IsMatch(line, $"^{name}{sep}"))
            {
                Assert.Matches($"{name}{sep}{val}", line);
                names.Add(name);
                count++;
                return true;
            }
            return false;
        }
        [Fact]
        public void TestRdpDefaultTemplate()
        {
            //Rdp string
            //rdp://username=s:encodeduser&full+address=s:hostname:port&screen%20mode%20id=i:3&shell working directory=s:C:/dir1 dir2
            using (var sut = new DefaultRdpUrlParser(new Dto.ParserConfig { UseDefaultTemplate = true }))
            {
                //Test default rdp settings
                // any rdp settings in the url will be preserved and will override the hardcoded defaults
                var altshell = "||OISGRemoteAppLauncher (1)";
                var rempgm = "||OISGRemoteAppLauncher (1)";
                var remname = "TestRemoteApp";
                var settings = "alternate+shell:s:%7C%7COISGRemoteAppLauncher%20%281%29&remoteapplicationprogram:s:%7C%7COISGRemoteAppLauncher%20%281%29&remoteapplicationname:s:TestRemoteApp";

                var url = $"rdp://{settings}&username=s:my%20test%20user%5cishere&full%20address=s:myhostname:3333&screen%20mode%20id=i:3&shell+working+directory=s:C%3a%2Fdir1+dir2/";
                var dictionary = sut.Parse($"{url}");
                Assert.Equal(url, dictionary[Token.OriginalUrl]);
                Assert.Equal("rdp", dictionary[Token.Protocol]);
                Assert.Equal("myhostname", dictionary[Token.Host]);
                Assert.Equal("3333", dictionary[Token.Port]);
                Assert.Equal("my test user\\ishere", dictionary[Token.User]);
                Assert.Equal(altshell, dictionary[Token.AlternateShell]);
                Assert.Equal(rempgm, dictionary[Token.Remoteapplicationprogram]);
                Assert.Equal(remname, dictionary[Token.Remoteapplicationname]);
                var tempfile = dictionary[Token.GeneratedFile];
                var lines = File.ReadAllLines(tempfile);
                var count = 0;
                var names = new List<string>();
                foreach (var one in lines)
                {
                    if (check(one, "screen mode id", "i:", names, ref count)) continue;
                    if (check(one, "shell working directory", "s:C:/dir1 dir2", names, ref count)) continue;
                    if (check(one, "full address", "s:myhostname:3333", names, ref count)) continue;
                    if (check(one, "username", "s:my test user" + Regex.Escape("\\") + "ishere", names, ref count)) continue;
                    if (check(one, "alternate shell", $"s:{altshell}", names, ref count)) continue;
                    if (check(one, "remoteapplicationprogram", $"s:{rempgm}", names, ref count))continue;
                    if (check(one, "remoteapplicationname", $"s:{remname}", names, ref count)) continue;
                    if (check(one, "password 51", $"b:\\S+", names, ref count)) continue;
                }
                var exp = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? 8 : 7;
                Assert.True(exp==count, "Matched names: " + string.Join(",", names));
            }
        }

        [Fact]
        public void TestRdpTemplate1()
        {
            var template = Path.GetTempFileName();
            //test using an rdp template file
            //only values in teh template are included - if not value specified, use the value passed in the url

            var lines = new List<string>
            {
                "screen mode id:i:1111",
                "shell working directory:s:C:/dir1 dir2",
                $"full address:s:%{Token.Host}%:%{Token.Port}%",
                $"username:s:%{Token.User}%",             
                $"singlemoninwindowedmode:i:0",
                $"alternate shell:s:testanothershellOn %Host%",
                $"remoteapplicationprogram:s:",
                "password 51:b:keepthis"
            };
            File.WriteAllLines(template, lines);

            using (var sut = new DefaultRdpUrlParser(new Dto.ParserConfig { UseTemplateFile = template }))
            {
                var settings = "alternate+shell:s:%7C%7COISGRemoteAppLauncher%20%281%29&remoteapplicationprogram:s:%7C%7COISGRemoteAppLauncher%20%281%29&remoteapplicationname:s:TestRemoteApp";
                var altshell = "||OISGRemoteAppLauncher (1)";
                var rempgm = "||OISGRemoteAppLauncher (1)";
                var remname = "TestRemoteApp";
                var url = $"rdp://{settings}&username=s:my test user%5cishere&full%20address=s:myhostname:3333&screen%20mode%20id=i:3&shell+working+directory=s:C%3a%2Fdir1+dir2/";
                var dictionary = sut.Parse($"{url}");
                Assert.Equal(url, dictionary[Token.OriginalUrl]);
                Assert.Equal("rdp", dictionary[Token.Protocol]);
                Assert.Equal("myhostname", dictionary[Token.Host]);
                Assert.Equal("3333", dictionary[Token.Port]);
                Assert.Equal("my test user\\ishere", dictionary[Token.User]);
                Assert.Equal(altshell, dictionary[Token.AlternateShell]);
                Assert.Equal(rempgm, dictionary[Token.Remoteapplicationprogram]);
                Assert.Equal(remname, dictionary[Token.Remoteapplicationname]);

                var tempfile = dictionary[Token.GeneratedFile];
                var fileLines = File.ReadAllLines(tempfile);
                var count = 0;
                var names = new List<string>();

                foreach (var one in fileLines)
                {

                    if (check(one, "screen mode id", "i:1111", names, ref count)) continue;
                    if (check(one, "shell working directory", "s:C:/dir1 dir2", names, ref count)) continue;
                    if (check(one, "full address", "s:myhostname:3333", names, ref count)) continue;
                    if (check(one, "username", "s:my test user" + Regex.Escape("\\") + "ishere", names, ref count)) continue;
                    if (check(one, "singlemoninwindowedmode", "i:0", names, ref count)) continue;
                    if (check(one, "alternate shell", $"s:testanothershellOn myhostname", names, ref count)) continue;
                    if (check(one, "password 51", "b:keepthis", names, ref count)) continue;
                    if (check(one, "remoteapplicationprogram", $"s:{rempgm}", names, ref count)) continue;

                }
                Assert.True(count == 8, "Matched names: " + string.Join(",", names));
                Assert.Equal(count, fileLines.Length);
            }
            if (File.Exists(template))
            {
                File.Delete(template);
            }
        }


        [Fact]
        public void TestRdpTemplate2()
        {
            var template = Path.GetTempFileName();
            
            //generic template - not ms rdp
            var lines = new List<string>
            {
                $"MyAddress=%{Token.Host}%:%{Token.Port}%",
                $"User=%{Token.User}%",  
                $"Shell=%{Token.AlternateShell}%_%{Token.Host}%",
                $"Name=%{Token.Remoteapplicationname}%_%{Token.Host}%",
                $"Exe=%{Token.Remoteapplicationprogram}%_%{Token.Host}%"
            };
            File.WriteAllLines(template, lines);

            //test using an rdp template file
            //any rdp settings in the url will be preserved and will override the values in the template
            using (var sut = new DefaultRdpUrlParser(new Dto.ParserConfig { UseTemplateFile = template }))
            {
                var settings = "alternate+shell:s:%7C%7COISGRemoteAppLauncher%20%281%29&remoteapplicationprogram:s:%7C%7COISGRemoteAppLauncher%20%281%29&remoteapplicationname:s:TestRemoteApp";
                var altshell = "||OISGRemoteAppLauncher (1)";
                var rempgm = "||OISGRemoteAppLauncher (1)";
                var remname = "TestRemoteApp";
                var url = $"rdp://{settings}&username=s:my test user%5cishere&full%20address=s:myhostname:3333&screen%20mode%20id=i:3&shell+working+directory=s:C%3a%2Fdir1+dir2/";
                var dictionary = sut.Parse($"{url}");
                Assert.Equal(url, dictionary[Token.OriginalUrl]);
                Assert.Equal("rdp", dictionary[Token.Protocol]);
                Assert.Equal("myhostname", dictionary[Token.Host]);
                Assert.Equal("3333", dictionary[Token.Port]);
                Assert.Equal("my test user\\ishere", dictionary[Token.User]);
                Assert.Equal(altshell, dictionary[Token.AlternateShell]);
                Assert.Equal(rempgm, dictionary[Token.Remoteapplicationprogram]);
                Assert.Equal(remname, dictionary[Token.Remoteapplicationname]);

                var tempfile = dictionary[Token.GeneratedFile];
                var fileLines = File.ReadAllLines(tempfile);
                var count = 0;
                var names = new List<string>();

                foreach (var one in fileLines)
                {
                    if (check(one, "User", "my test user" + Regex.Escape("\\") + "ishere", names, ref count, "=")) continue;
                    if (check(one, "MyAddress", "myhostname:3333", names, ref count, "=")) continue;
                    if (check(one, "Shell", $"{altshell}_myhostname", names, ref count, "=")) continue;
                    if (check(one, "Exe", $"{rempgm}_myhostname", names, ref count, "=")) continue;
                    if (check(one, "Name", $"{remname}_myhostname", names, ref count, "=")) continue;
                                 
                }
                Assert.Equal(5, count);
                Assert.Equal(count, fileLines.Length);
            }
            if (File.Exists(template))
            {
                File.Delete(template);
            }
        }
        [Fact]
        public void Test4()
        {

            //somerandomstring
            var sut = new DefaultRdpUrlParser(new Dto.ParserConfig());
            var str = "somerandomstring@here";
            Assert.ThrowsAny<Exception>(() => sut.Parse(str));


            //standard URI
            //myprot://my test user:mypass@myhost:2222/thisisapath?queryit#fragment
            using (sut = new DefaultRdpUrlParser(new Dto.ParserConfig()))
            {

                str = "my%20test%20user%5c:mypass@myhost:3456/thisisapath?queryit#fragment";
                //var decodedStr = "my test user\\:mypass@myhost:3456/thisisapath?queryit#fragment";
                var uri = $"myprot://{str}";

                var dictionary = sut.Parse(uri);
                Assert.Equal("myprot", dictionary[Token.Protocol]);
                Assert.Equal(uri, dictionary[Token.OriginalUrl]);
                Assert.Equal(str, dictionary[Token.RelativeUrl]);
                Assert.Equal("my test user\\:mypass", dictionary[Token.User]);
                Assert.Equal("myhost", dictionary[Token.Host]);
                Assert.Equal("3456", dictionary[Token.Port]);
                Assert.Equal("thisisapath", dictionary[Token.Path]);
                Assert.Equal("queryit", dictionary[Token.Query]);
                Assert.Equal("fragment", dictionary[Token.Fragment]);
                Assert.False(dictionary.ContainsKey(Token.GeneratedFile));
            }
        }
        [Fact]
        public void Test5()
        {
            using (var sut = new DefaultTelnetUrlParser(new Dto.ParserConfig()))
            {
                var dictionary = sut.Parse("tel://myuser@myhost");
                Assert.Equal("tel://myuser@myhost", dictionary[Token.OriginalUrl]);
                Assert.Equal("myuser@myhost", dictionary[Token.RelativeUrl]);
                Assert.Equal("tel", dictionary[Token.Protocol]);
                Assert.Equal("myuser", dictionary[Token.User]);
                Assert.Equal("myhost", dictionary[Token.Host]);
            }

        }
        [Fact]
        public void Test6()
        {
            using (var sut = new UrlParser.UrlParser(new Dto.ParserConfig()))
            {
                var dictionary = sut.Parse("customprotocol://user:password@www.myhost.com:111/mylocation");
                Assert.Equal("customprotocol://user:password@www.myhost.com:111/mylocation", dictionary[Token.OriginalUrl]);
                Assert.Equal("user:password@www.myhost.com:111/mylocation", dictionary[Token.RelativeUrl]);
                Assert.Equal("customprotocol", dictionary[Token.Protocol]);
                Assert.Equal("user:password", dictionary[Token.User]);
                Assert.Equal("www.myhost.com", dictionary[Token.Host]);
                Assert.Equal("mylocation", dictionary[Token.Path]);
                Assert.Equal("111", dictionary[Token.Port]);
            }

        }

        [Fact]
        public void TestSafeguardUser()
        {
            var account = "testuser";

            var asset = "TestRemoteApp";
            var vault = "10.5.34.64";
            var token = "9u69PNxT6PjSGU3DBeGGi1iGsc6sQhR3N73H1VjHFFYxvL2wY4WmtCMzR7C4Nj5wM9BXkiuwvAwyWw3TCj";
            var targetaccount = "bnicholes";
            var targethost = "10.5.37.52";

            var username = $"dan.vas\\account~{account}%asset~{asset}%vaultaddress~{vault}%token~{token}@{targetaccount}%{targethost}";

            var dict = new Dictionary<Token, string>() { { Token.User, username } };
            BaseParser.GetSafeguardUserValue(dict);
            Assert.Equal(account, dict[Token.Account]);
            Assert.Equal(asset, dict[Token.Asset]);
            Assert.Equal(vault, dict[Token.Vault]);
            Assert.Equal(token, dict[Token.Token]);
            Assert.Equal(targetaccount, dict[Token.TargetUser]);
            Assert.Equal(targethost, dict[Token.TargetHost]);

        }
      
    }
}
