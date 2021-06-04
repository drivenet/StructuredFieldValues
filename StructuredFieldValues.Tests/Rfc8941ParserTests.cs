using System;

using Xunit;

namespace StructuredFieldValues.Tests
{
    public class Rfc8941ParserTests
    {
        [Theory]
        [InlineData("?0", false)]
        [InlineData("?1", true)]
        [InlineData("?092379f&((*&3", false)]
        [InlineData("?1dmfsldjf*2834y392", true)]
        [InlineData("0", 0.0)]
        [InlineData("1", 1.0)]
        [InlineData("-17", -17.0)]
        [InlineData("2873913q123", 2873913.0)]
        [InlineData("1zxcc", 1.0)]
        [InlineData("-913vwe", -913.0)]
        [InlineData("1239712839", 1239712839.0)]
        [InlineData("1328409328402340", 1328409328402340.0)]
        [InlineData("484944311926.6", 484944311926.6)]
        [InlineData("472389478934.123", 472389478934.123)]
        [InlineData("987654321098765", 987654321098765.0)]
        [InlineData("\"\"", "")]
        [InlineData("\"!\"", "!")]
        [InlineData("\"abc def\"", "abc def")]
        [InlineData("\"d34234efghi\"qwjeoiwqe", "d34234efghi")]
        [InlineData("\"abc\\\\ def\"", "abc\\ def")]
        [InlineData("\"quotes \\\"72893d\\\" wejp18 \"", "quotes \"72893d\" wejp18 ")]
        [InlineData("TOK3N", "TOK3N")]
        [InlineData("*Test-Token.", "*Test-Token.")]
        [InlineData("TestT0ken\tWithTabs", "TestT0ken")]
        [InlineData("*!#$%^&+-.~'`^_|~", "*!#$%^&+-.~'`^_|~")]
        [InlineData("*!@#$%^&+-.~'`^_|~", "*!")]
        [InlineData("*!#$%^&+-.~'`^_@|~", "*!#$%^&+-.~'`^_")]
        public void ParseBareItemWorks(string data, object value)
        {
            Assert.Equal(value, Rfc8941Parser.ParseBareItem(data).Unwrap());
        }

        [Theory]
        [InlineData("?0", false)]
        [InlineData("?1", true)]
        [InlineData("?092379f&((*&3", false)]
        [InlineData("?1dmfsldjf*2834y392", true)]
        public void ParseBooleanWorks(string data, bool value)
        {
            Assert.Equal(value, Rfc8941Parser.ParseBoolean(data).Unwrap());
        }

        [Theory]
        [InlineData("")]
        [InlineData("true")]
        [InlineData("?2")]
        [InlineData("false")]
        public void ParseBooleanFailsCorrectly(string data)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseBoolean(data).Unwrap());
        }

        [Theory]
        [InlineData("0", 0.0)]
        [InlineData("1", 1.0)]
        [InlineData("-17", -17.0)]
        [InlineData("2873913q123", 2873913.0)]
        [InlineData("1zxcc", 1.0)]
        [InlineData("-913vwe", -913.0)]
        [InlineData("1239712839", 1239712839.0)]
        [InlineData("1328409328402340", 1328409328402340.0)]
        [InlineData("484944311926.6", 484944311926.6)]
        [InlineData("472389478934.123", 472389478934.123)]
        [InlineData("987654321098765", 987654321098765.0)]
        public void ParseNumberWorks(string data, double value)
        {
            Assert.Equal(value, Rfc8941Parser.ParseNumber(data).Unwrap());
        }

        [Theory]
        [InlineData("472389478934.1234")]
        [InlineData("9876543210987654.")]
        [InlineData("9876543210981.0")]
        [InlineData("9876543210987.123")]
        [InlineData("98765432109876.12")]
        [InlineData("987654321098765.1")]
        public void ParseNumberFailsCorrectly(string data)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseNumber(data).Unwrap());
        }

        [Theory]
        [InlineData("\"\"", "")]
        [InlineData("\"!\"", "!")]
        [InlineData("\"abc def\"", "abc def")]
        [InlineData("\"d34234efghi\"qwjeoiwqe", "d34234efghi")]
        [InlineData("\"abc\\\\ def\"", "abc\\ def")]
        [InlineData("\"quotes \\\"72893d\\\" wejp18 \"", "quotes \"72893d\" wejp18 ")]
        public void ParseStringWorks(string data, string value)
        {
            Assert.Equal(value, Rfc8941Parser.ParseString(data).Unwrap());
        }

        [Theory]
        [InlineData("")]
        [InlineData("\"")]
        [InlineData("a bc\"")]
        [InlineData("\"d34234e fghiqwjeoiwqe")]
        [InlineData("\"тест\"")]
        [InlineData("\"cr\r\nlf\"")]
        [InlineData("\"quotes \\Q72893d\\\" wejp18 \"")]
        [InlineData("\"escaping \\")]
        public void ParseStringFailsCorrectly(string data)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseString(data).Unwrap());
        }

        [Theory]
        [InlineData("TOK3N", "TOK3N")]
        [InlineData("*Test-Token.", "*Test-Token.")]
        [InlineData("TestT0ken\tWithTabs", "TestT0ken")]
        [InlineData("*!#$%^&+-.~'`^_|~", "*!#$%^&+-.~'`^_|~")]
        [InlineData("*!@#$%^&+-.~'`^_|~", "*!")]
        [InlineData("*!#$%^&+-.~'`^_@|~", "*!#$%^&+-.~'`^_")]
        public void ParseTokenWorks(string data, string value)
        {
            Assert.Equal(value, Rfc8941Parser.ParseToken(data).Unwrap());
        }

        [Theory]
        [InlineData("")]
        [InlineData(" TEST")]
        [InlineData("\"TEST\"")]
        [InlineData("!")]
        [InlineData("7 Test")]
        [InlineData("1Token")]
        [InlineData("9AFUH162")]
        public void ParseTokenFailsCorrectly(string data)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseToken(data).Unwrap());
        }
    }
}
