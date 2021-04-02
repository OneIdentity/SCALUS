using scalus.UrlParser;
using Xunit;

namespace scalus.Test
{
    public class TestDefaultSshUrlParser
    {
        [Fact]
        public void Test1()
        {
            var sut = new DefaultSshUrlParser(new Dto.ParserConfig());
            var result = sut.Parse("key=value%40whatever%4012.23.43.2@1.2.3.4:33");
            Assert.Equal("key=value@whatever@12.23.43.2", result["user"]);
            Assert.Equal("1.2.3.4", result["host"]);
            Assert.Equal("33", result["port"]);
        }
    }
}
