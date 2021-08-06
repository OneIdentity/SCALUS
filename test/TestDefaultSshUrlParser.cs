using System    ;
using System.Web;
using scalus.UrlParser;
using Xunit;
using static scalus.Dto.ParserConfigDefinitions;

namespace scalus.Test
{
    public class TestDefaultSshUrlParser
    {
        [Fact]
        public void Test1()
        {
            //generic user@host
            using (var sut = new DefaultSshUrlParser(new Dto.ParserConfig()))
            {
                var str="key=value%40whatever%4012.23.43.2@1.2.3.4:33";
                var dictionary = sut.Parse(str);
                Assert.Equal("ssh", dictionary[Token.Protocol]);
                Assert.Equal(str, dictionary[Token.OriginalUrl]);
                Assert.Equal(str, dictionary[Token.RelativeUrl]);
                Assert.Equal("key=value@whatever@12.23.43.2", dictionary[Token.User]);
                Assert.Equal("1.2.3.4", dictionary[Token.Host]);
                Assert.Equal("33", dictionary[Token.Port]);
            }

            //ssh://user@host
            using (var sut = new DefaultSshUrlParser(new Dto.ParserConfig()))
            {
                var str="key=value%40whatever%4012.23.43.2@1.2.3.4:33";
                var dictionary = sut.Parse($"ssh://{str}");

                Assert.Equal("ssh", dictionary[Token.Protocol]);
                Assert.Equal($"ssh://{str}", dictionary[Token.OriginalUrl]);
                Assert.Equal(str, dictionary[Token.RelativeUrl]);
                Assert.Equal("key=value@whatever@12.23.43.2", dictionary[Token.User]);
                Assert.Equal("1.2.3.4", dictionary[Token.Host]);
                Assert.Equal("33", dictionary[Token.Port]);

                str = "ssh://root%4010.1.1.1";
                dictionary = sut.Parse(str);
            }
        }

        [Fact]
        public void Test2()
        {

            //ssh://safeguarduserstring@host
            using (var sut = new DefaultSshUrlParser(new Dto.ParserConfig()))
            {
                //vaultaddress=(.*)@token=(.*)@(.*)@(.*)@(.*)
                var prot = "ssh";
                var vault = "10.5.33.84";
                var token = "47EuUuRU7Rp5cSWVb61GFWNQhvHrHC1RwT8PtocfNeT1LshtjzChDWYsEbY9LC5wX8MhT5rZXSsCK7iyfs2x6";
                var user = "mim%20test%201";
                var decodedUser = "mim test 1";
                var target = "10.4.34.42";
                var port = "222";
                var sps = "10.5.33.88";
                var str = $"vaultaddress={vault}@token={token}@{user}@{target}@{sps}:{port}";
                var uri = $"{prot}://{str}";

                var dictionary = sut.Parse(uri);
                Assert.Equal(prot, dictionary[Token.Protocol]);
                Assert.Equal(uri, dictionary[Token.OriginalUrl]);
                Assert.Equal(str, dictionary[Token.RelativeUrl]);
                Assert.Equal(vault, dictionary[Token.Vault]);
                Assert.Equal(token, dictionary[Token.Token]);
                Assert.Equal(decodedUser, dictionary[Token.TargetUser]);
                Assert.Equal(target, dictionary[Token.TargetHost]);
                Assert.Equal(port, dictionary[Token.Port]);
                Assert.Equal(sps, dictionary[Token.Host]);
                Assert.Equal($"vaultaddress={vault}@token={token}@{decodedUser}@{target}", dictionary[Token.User]);

                str = "scp://a@b:2";
                dictionary = sut.Parse(str);
                Assert.Equal("scp", dictionary[Token.Protocol]);
                Assert.Equal("a", dictionary[Token.User]);
                Assert.Equal("b", dictionary[Token.Host]);
                Assert.Equal("2", dictionary[Token.Port]);

                //optional port number
                str = $"{prot}://vaultaddress={vault}@token={token}@{user}@{target}@{sps}";
                dictionary = sut.Parse(str);
                Assert.Equal("22", dictionary[Token.Port]);
                Assert.Equal(sps, dictionary[Token.Host]);

                //encoded user 
                str = $"{prot}://vaultaddress={vault}%40token={token}%40{user}%40{target}@{sps}:{port}";
                dictionary = sut.Parse(str);
                Assert.Equal(str, dictionary[Token.OriginalUrl]);
                Assert.Equal(vault, dictionary[Token.Vault]);
                Assert.Equal(token, dictionary[Token.Token]);
                Assert.Equal(decodedUser, dictionary[Token.TargetUser]);
                Assert.Equal(target, dictionary[Token.TargetHost]);
                Assert.Equal(port, dictionary[Token.Port]);
                Assert.Equal(sps, dictionary[Token.Host]);
                Assert.Equal($"vaultaddress={vault}@token={token}@{decodedUser}@{target}", dictionary[Token.User]);


                //encoded user 
                str = $"{prot}://vaultaddress={vault}%40token={token}%40{user}%40{target}%40{sps}%3a{port}";
                dictionary = sut.Parse(str);
                Assert.Equal(str, dictionary[Token.OriginalUrl]);
                Assert.Equal(vault, dictionary[Token.Vault]);
                Assert.Equal(token, dictionary[Token.Token]);
                Assert.Equal(decodedUser, dictionary[Token.TargetUser]);
                Assert.Equal(target, dictionary[Token.TargetHost]);
                Assert.Equal(port, dictionary[Token.Port]);
                Assert.Equal(sps, dictionary[Token.Host]);
                Assert.Equal($"vaultaddress={vault}@token={token}@{decodedUser}@{target}", dictionary[Token.User]);



                var userstr =
                    "vaultaddress%3D10.5.33.84%40token%3D4AenuEc5PcyGUhB5Y68L7qzysoCbvDDATuuKpM7JQvVPCo3S18zGUPZziC9cQa3kgjLfUvD3ND8RFAxpAk2Yq%40mim%20test%201%4010.5.34.42";
                str = $"ssh://{userstr}%4010.5.33.88";

                dictionary = sut.Parse(str);
                Assert.Equal("10.5.33.84", dictionary[Token.Vault]);
                Assert.Equal("4AenuEc5PcyGUhB5Y68L7qzysoCbvDDATuuKpM7JQvVPCo3S18zGUPZziC9cQa3kgjLfUvD3ND8RFAxpAk2Yq", dictionary[Token.Token]);
                Assert.Equal("mim test 1", dictionary[Token.TargetUser]);
                Assert.Equal("10.5.34.42", dictionary[Token.TargetHost]);
                Assert.Equal("10.5.33.88", dictionary[Token.Host]);
                Assert.Equal(HttpUtility.UrlDecode(userstr), dictionary[Token.User]);


                str = $"ssh://mim%20test%201%4010.5.33.88";

                dictionary = sut.Parse(str);
                Assert.Equal("mim test 1", dictionary[Token.User]);
                Assert.Equal("10.5.33.88", dictionary[Token.Host]);
            }

        }

        [Fact]
        public void Test3()
        {
            var str = "abc";
            using (var sut = new DefaultSshUrlParser(new Dto.ParserConfig())){
            Assert.ThrowsAny<Exception>(() => sut.Parse(str));

            str = "ssh://abc/";
            var dictionary =sut.Parse(str);
            Assert.Equal("ssh://abc/", dictionary[Token.OriginalUrl]);
            Assert.Equal("abc/", dictionary[Token.RelativeUrl]);
            Assert.Equal("ssh", dictionary[Token.Protocol]);
            }
        }
        
    }
}
