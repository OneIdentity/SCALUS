using Sulu.UrlParser;
using System;
using Xunit;

namespace Sulu.Test
{
    public class TestDefaultRdpUrlParser
    {
        [Fact]
        public void Test1()
        {
            var sut = new DefaultRdpUrlParser();
            var result = sut.Parse("rdp://full+address=s:10.5.32.168:3389&username=s:localhost%5cvaultaddress%7e10.5.33.238%25token%7epdFwRQSofwL6xJV4Ud32g4TXKM7XgXkYU8ks4i5GQHURRoBiFq5Rjr4dT%25win10-acct1%2510.5.60.94:3389/");
            Assert.Equal(@"localhost\vaultaddress~10.5.33.238%token~pdFwRQSofwL6xJV4Ud32g4TXKM7XgXkYU8ks4i5GQHURRoBiFq5Rjr4dT%win10-acct1%10.5.60.94:3389", result["$user"]);
            Assert.Equal("10.5.32.168", result["$host"]);
            Assert.Equal("3389", result["$port"]);
        }
    }
}
