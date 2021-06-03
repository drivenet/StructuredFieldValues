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
        public void ParseBareItemWorks(string data, bool value)
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
        [InlineData("0", 0)]
        [InlineData("1", 1)]
        [InlineData("-17", -17)]
        [InlineData("2873913q123", 2873913)]
        [InlineData("1zxcc", 1)]
        [InlineData("-913vwe", -913)]
        [InlineData("1239712839", 1239712839)]
        [InlineData("1328409328402340", 1328409328402340.0)]
        [InlineData("472389478934.123", 472389478934.123)]
        public void ParseNumberWorks(string data, double value)
        {
            Assert.Equal(value, Rfc8941Parser.ParseNumber(data).Unwrap());
        }

        [Theory]
        [InlineData("472389478934.1234")]
        public void ParseNumberFailsCorrectly(string data)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseNumber(data).Unwrap());
        }
    }
}
