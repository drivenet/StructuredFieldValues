﻿using System;

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
        [InlineData("::", "")]
        [InlineData(":YWpyOTgyMzd5czdyZXkzd3I=:", "YWpyOTgyMzd5czdyZXkzd3I=")]
        public void ParseBareItemWorks(string data, object value)
        {
            var result = Rfc8941Parser.ParseBareItem(data).Unwrap();
            if (result is ReadOnlyMemory<byte> bytes)
            {
                result = Convert.ToBase64String(bytes.ToArray());
            }

            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData("?0", 0, false, 2)]
        [InlineData("?1", 0, true, 2)]
        [InlineData("?092379f&((*&3", 0, false, 2)]
        [InlineData("?1dmfsldjf*2834y392", 0, true, 2)]
        [InlineData("?1?0some", 2, false, 4)]
        [InlineData("?0?1some", 2, true, 4)]
        [InlineData("?1?092379f&((*&3", 2, false, 4)]
        [InlineData("?0?1dmfsldjf*2834y392", 2, true, 4)]
        [InlineData("some?0!", 4, false, 6)]
        [InlineData("some?1!", 4, true, 6)]
        public void ParseBooleanWorks(string data, int index, bool value, int lastIndex)
        {
            Assert.Equal(value, Rfc8941Parser.ParseBoolean(data, ref index).Unwrap());
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("?", 0, 1)]
        [InlineData("true", 0, 0)]
        [InlineData("?2", 0, 1)]
        [InlineData("false", 0, 0)]
        [InlineData("?0as", 1, 1)]
        [InlineData("?0dc!", 2, 2)]
        public void ParseBooleanFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseBoolean(data, ref index).Unwrap());
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("0", 0, 0.0, 1)]
        [InlineData("1", 0, 1.0, 1)]
        [InlineData("-17", 0, -17.0, 3)]
        [InlineData("2873913q123", 0, 2873913.0, 7)]
        [InlineData("q1232873913", 4, 2873913.0, 11)]
        [InlineData("1zxcc", 0, 1.0, 1)]
        [InlineData("-913vwe", 0, -913.0, 4)]
        [InlineData("1239712839", 0, 1239712839.0, 10)]
        [InlineData("1328409328402340", 0, 1328409328402340.0, 16)]
        [InlineData("484944311926.6", 0, 484944311926.6, 14)]
        [InlineData("472389478934.123", 0, 472389478934.123, 16)]
        [InlineData("987654321098765", 0, 987654321098765.0, 15)]
        public void ParseNumberWorks(string data, int index, double value, int lastIndex)
        {
            Assert.Equal(value, Rfc8941Parser.ParseNumber(data, ref index).Unwrap());
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("472389478934.1234", 0, 16)]
        [InlineData("9876543210987654.", 0, 16)]
        [InlineData("9876543210981.0", 0, 13)]
        [InlineData("9876543210987.123", 0, 13)]
        [InlineData("98765432109876.12", 0, 14)]
        [InlineData("987654321098765.1", 0, 15)]
        [InlineData("number", 0, 0)]
        [InlineData("9876543210number", 10, 10)]
        public void ParseNumberFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseNumber(data, ref index).Unwrap());
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("\"\"", 0, "", 2)]
        [InlineData("\"!\"", 0, "!", 3)]
        [InlineData("\"abc def\"", 0, "abc def", 9)]
        [InlineData("\"r0x\"abc def\"", 4, "abc def", 13)]
        [InlineData("\"d34234efghi\"qwjeoiwqe", 0, "d34234efghi", 13)]
        [InlineData("\"abc\\\\ def\"", 0, "abc\\ def", 11)]
        [InlineData("\"quotes \\\"72893d\\\" wejp18 \"", 0, "quotes \"72893d\" wejp18 ", 27)]
        [InlineData("qwieu189xHH\"d34234ghi\"qwjiwqe", 11, "d34234ghi", 22)]
        public void ParseStringWorks(string data, int index, string value, int lastIndex)
        {
            Assert.Equal(value, Rfc8941Parser.ParseString(data, ref index).Unwrap());
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("\"", 0, 1)]
        [InlineData("a bc\"", 0, 0)]
        [InlineData("\"d34234e fghiqwjeoiwqe", 0, 22)]
        [InlineData("\"тест\"", 0, 1)]
        [InlineData("\"ewqe2x\"", 1, 1)]
        [InlineData("\"ewqe\"2x", 5, 8)]
        [InlineData("\"cr\r\nlf\"", 0, 3)]
        [InlineData("\"quotes \\Q72893d\\\" wejp18 \"", 0, 9)]
        [InlineData("\"escaping \\", 0, 11)]
        public void ParseStringFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseString(data, ref index).Unwrap());
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("TOK3N", 0, "TOK3N", 5)]
        [InlineData("*Test-Token.", 0, "*Test-Token.", 12)]
        [InlineData("TestT0ken\tWithTabs", 0, "TestT0ken", 9)]
        [InlineData("*!#$%^&+-.~'`^_|~", 0, "*!#$%^&+-.~'`^_|~", 17)]
        [InlineData("*!@#$%^&+-.~'`^_|~", 0, "*!", 2)]
        [InlineData("*!#$%^&+-.~'`^_@|~", 0, "*!#$%^&+-.~'`^_", 15)]
        public void ParseTokenWorks(string data, int index, string value, int lastIndex)
        {
            Assert.Equal(value, Rfc8941Parser.ParseToken(data, ref index).Unwrap());
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData(" TEST", 0, 0)]
        [InlineData("\"TEST\"", 0, 0)]
        [InlineData("!", 0, 0)]
        [InlineData("7 Test", 0, 0)]
        [InlineData("1T0ken", 2, 2)]
        [InlineData("r!dsX9AFUH162", 5, 5)]
        public void ParseTokenFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseToken(data, ref index).Unwrap());
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("::2u3y7", 0, "", 2)]
        [InlineData(":YWpyOTgyMzd5czdyZXkzd3I=:", 0, "YWpyOTgyMzd5czdyZXkzd3I=", 26)]
        [InlineData("xdo721::", 6, "", 8)]
        [InlineData("xc,o2!7:XiZocTI4KiZoZlxo:", 7, "XiZocTI4KiZoZlxo", 25)]
        public void ParseByteSequenceWorks(string data, int index, string value, int lastIndex)
        {
            Assert.Equal(value, Convert.ToBase64String(Rfc8941Parser.ParseByteSequence(data, ref index).Unwrap().ToArray()));
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData(":sdq", 0, 4)]
        [InlineData(" :", 0, 0)]
        [InlineData("Aaaa", 0, 0)]
        [InlineData(":!:", 0, 1)]
        [InlineData("addsad:*:", 6, 7)]
        public void ParseByteSequenceFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.Throws<FormatException>(() => Rfc8941Parser.ParseByteSequence(data, ref index).Unwrap());
            Assert.Equal(lastIndex, index);
        }
    }
}
