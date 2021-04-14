using scalus.Dto;
using scalus.UrlParser;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace scalus.Test
{
    public class TestDefaultRdpUrlParser
    {
        
        [Fact]
        public void Test1()
        {
            //Rdp string
            //full+address=s:host&username=s:safeguarduserstring
            var sut = new DefaultRdpUrlParser(new Dto.ParserConfig());

            
            var prot="rdp";
            var host="10.5.32.168";
            var port="3389";
            var vault="10.5.33.238";
            var token="epdFwRQSofwL6xJV4Ud32g4TXKM7XgXkYU8ks4i5GQHURRoBiFq5Rjr4dT";
            var targetuser="win10-acct1";
            var targethost="10.5.60.94";
            var targetport="3389";
            var user = $"username=s:localhost%5cvaultaddress%7e{vault}%25token%7e{token}%25{targetuser}%25{targethost}:{targetport}";
            var decodedUser = $"localhost\\vaultaddress~{vault}%token~{token}%{targetuser}%{targethost}:{targetport}";

            var str = $"full+address=s:{host}:{port}&{user}";
            var dictionary =sut.Parse($"{str}/");
            Assert.Equal($"{str}/", dictionary[Token.OriginalUrl]);
            Assert.Equal(str, dictionary[Token.RelativeUrl]);
            Assert.Equal(prot, dictionary[Token.Protocol]);
            Assert.Equal(host, dictionary[Token.Host]);
            Assert.Equal(port, dictionary[Token.Port]);
            Assert.Equal(decodedUser, dictionary[Token.User]);
            Assert.Equal(vault, dictionary[Token.Vault]);
            Assert.Equal(token, dictionary[Token.Token]);
            Assert.Equal(targetuser, dictionary[Token.TargetUser]);
            Assert.Equal(targethost, dictionary[Token.TargetHost]);
            Assert.Equal(targetport, dictionary[Token.TargetPort]);
            Assert.False(dictionary.ContainsKey(Token.GeneratedFile));

            //Rdp string
            //rdp://full address=s:host&username=s:user
            var uri = $"{prot}://{str}/";
            dictionary = sut.Parse($"{uri}");
            Assert.Equal(uri, dictionary[Token.OriginalUrl]);
            Assert.Equal(str, dictionary[Token.RelativeUrl]);
            Assert.Equal(prot, dictionary[Token.Protocol]);

        }

        [Fact]
        public void Test2()
        {
            //Rdp string
            //rdp://username=s:encodeduser&full+address=s:hostname:port&screen%20mode%20id=i:3&shell working directory=s:C:/dir1 dir2
            using (var sut = new DefaultRdpUrlParser(new Dto.ParserConfig{ UseDefaultTemplate = true }))
            {
                var url = "rdp://username=s:myuser%5cishere&full%20address=s:myhostname:3333&screen%20mode%20id=i:3&shell+working+directory=s:C%3a%2Fdir1+dir2/";
                var dictionary = sut.Parse($"{url}");
                Assert.Equal(url, dictionary[Token.OriginalUrl]);
                Assert.Equal("rdp", dictionary[Token.Protocol]);
                Assert.Equal("myhostname", dictionary[Token.Host]);
                Assert.Equal("3333", dictionary[Token.Port]);
                Assert.Equal("myuser\\ishere", dictionary[Token.User]);
                var tempfile = dictionary[Token.GeneratedFile ];
                var lines = File.ReadAllLines(tempfile);
                var count = 0;
                foreach(var one in lines)
                {
                    if (Regex.IsMatch(one, "^screen mode id:i:"))
                    {
                        Assert.Equal("screen mode id:i:3", one);
                        count++;
                    }
                    if (Regex.IsMatch(one,"^shell working directory" ))
                    {
                        count++;
                        Assert.Equal("shell working directory:s:C:/dir1 dir2", one);
                    }
                    if (Regex.IsMatch(one,"^full address" ))
                    {
                        count++;
                        Assert.Equal("full address:s:myhostname:3333", one);
                    }
                    if (Regex.IsMatch(one,"^username" ))
                    {
                        count++;
                        Assert.Equal("username:s:myuser\\ishere", one);
                    }
                }
            Assert.Equal(4, count);
            }     
        }

        [Fact]
        public void Test3()
        {
            //Rdp string
            //rdp://username=s:encodeduser&full+address=s:hostname:port&screen%20mode%20id=i:3&shell working directory=s:C:/dir1 dir2
            var template = Path.GetTempFileName();
            var lines = new List<string>
            {
                "screen mode id:i:1111",
                "shell working directory:s:C:/dir1 dir2",
                $"full address:s:%{Token.Host}%:%{Token.Port}%",
                $"username:s:%{Token.User}%"
            };
            File.WriteAllLines(template, lines);

            using (var sut = new DefaultRdpUrlParser(new Dto.ParserConfig{ UseTemplateFile = template }))
            {
                var url = "rdp://username=s:myuser%5cishere&full%20address=s:myhostname:3333&screen%20mode%20id=i:3&shell+working+directory=s:C%3a%2Fdir1+dir2/";
                var dictionary = sut.Parse($"{url}");
                Assert.Equal(url, dictionary[Token.OriginalUrl]);
                Assert.Equal("rdp", dictionary[Token.Protocol]);
                Assert.Equal("myhostname", dictionary[Token.Host]);
                Assert.Equal("3333", dictionary[Token.Port]);
                Assert.Equal("myuser\\ishere", dictionary[Token.User]);
                var tempfile = dictionary[Token.GeneratedFile ];
                var fileLines = File.ReadAllLines(tempfile);
                var count = 0;
                foreach(var one in fileLines)
                {
                    if (Regex.IsMatch(one, "^screen mode id:i:"))
                    {
                        Assert.Equal("screen mode id:i:1111", one);
                        count++;
                    }
                    else if (Regex.IsMatch(one,"^shell working directory" ))
                    {
                        count++;
                        Assert.Equal("shell working directory:s:C:/dir1 dir2", one);
                    }
                    else if (Regex.IsMatch(one,"^full address" ))
                    {
                        count++;
                        Assert.Equal("full address:s:myhostname:3333", one);
                    }
                    else if (Regex.IsMatch(one,"^username" ))
                    {
                        count++;
                        Assert.Equal("username:s:myuser\\ishere", one);
                    }
                }
                Assert.Equal(4, count);
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
            var dictionary = sut.Parse(str);
            Assert.Equal(str, dictionary[Token.OriginalUrl]);
            Assert.Equal(str, dictionary[Token.RelativeUrl]);
            Assert.Null(dictionary[Token.Protocol]);

            //standard URI
            //myprot://myuser:mypass@myhost:2222/thisisapath?queryit#fragment
            sut = new DefaultRdpUrlParser(new Dto.ParserConfig());

            str="myuser%5c:mypass@myhost:3456/thisisapath?queryit#fragment";
            var uri=$"myprot://{str}";

            dictionary = sut.Parse(uri);
            Assert.Equal("myprot", dictionary[Token.Protocol]);
            Assert.Equal(uri, dictionary[Token.OriginalUrl]);
            Assert.Equal(str, dictionary[Token.RelativeUrl]);
            Assert.Equal("myuser%5c:mypass", dictionary[Token.User]);
            Assert.Equal("myhost", dictionary[Token.Host]);
            Assert.Equal("3456", dictionary[Token.Port]);
            Assert.Equal("thisisapath", dictionary[Token.Path]);
            Assert.Equal("queryit", dictionary[Token.Query]);
            Assert.Equal("fragment", dictionary[Token.Fragment]);          
            Assert.False(dictionary.ContainsKey(Token.GeneratedFile));

        }
        
    }
}
