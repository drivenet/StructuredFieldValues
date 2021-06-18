using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Xunit;
using Xunit.Sdk;

namespace StructuredFieldValues.Tests
{
    public class Rfc8941ParserTests
    {
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
        [InlineData("0", 0, 0L, 1)]
        [InlineData("1", 0, 1L, 1)]
        [InlineData("-17", 0, -17L, 3)]
        [InlineData("2873913q123", 0, 2873913L, 7)]
        [InlineData("q1232873913", 4, 2873913L, 11)]
        [InlineData("1zxcc", 0, 1L, 1)]
        [InlineData("-913vwe", 0, -913L, 4)]
        [InlineData("1239712839", 0, 1239712839L, 10)]
        [InlineData("1328409328402340", 0, 1328409328402340L, 16)]
        [InlineData("484944311926.6", 0, 484944311926.6, 14)]
        [InlineData("472389478934.123", 0, 472389478934.123, 16)]
        [InlineData("987654321098765", 0, 987654321098765L, 15)]
        [InlineData("\"\"", 0, "", 2)]
        [InlineData("\"!\"", 0, "!", 3)]
        [InlineData("\"abc def\"", 0, "abc def", 9)]
        [InlineData("\"r0x\"abc def\"", 4, "abc def", 13)]
        [InlineData("\"d34234efghi\"qwjeoiwqe", 0, "d34234efghi", 13)]
        [InlineData("\"abc\\\\ def\"", 0, "abc\\ def", 11)]
        [InlineData("\"quotes \\\"72893d\\\" wejp18 \"", 0, "quotes \"72893d\" wejp18 ", 27)]
        [InlineData("qwieu189xHH\"d34234ghi\"qwjiwqe", 11, "d34234ghi", 22)]
        [InlineData("TOK/417", 0, "TOK/417", 7)]
        [InlineData("*Test-Token.", 0, "*Test-Token.", 12)]
        [InlineData("Test:T0ken\tWithTabs", 0, "Test:T0ken", 10)]
        [InlineData("*!#$%^&:+-./~'`^_|~", 0, "*!#$%^&:+-./~'`^_|~", 19)]
        [InlineData("*!@#$/:%^&+-.~'`^_|~", 0, "*!", 2)]
        [InlineData("*!#$%^&+-.~'`^_@|~", 0, "*!#$%^&+-.~'`^_", 15)]
        [InlineData("::2u3y7", 0, "", 2)]
        [InlineData(":YWpyOTgyMzd5czdyZXkzd3I=:", 0, "YWpyOTgyMzd5czdyZXkzd3I=", 26)]
        [InlineData("xdo721::", 6, "", 8)]
        [InlineData("xc,o2!7:XiZocTI4KiZoZlxo:", 7, "XiZocTI4KiZoZlxo", 25)]
        public void ParseBareItemWorks(string data, int index, object value, int lastIndex)
        {
            Assert.Null(Rfc8941Parser.ParseBareItem(data, ref index, out var result));
            if (result is ReadOnlyMemory<byte> bytes)
            {
                result = Convert.ToBase64String(bytes.ToArray());
            }

            Assert.Equal(value, result, ReverseEqualityComparer<object>.Instance);
            Assert.Equal(lastIndex, index);
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
            Assert.Null(Rfc8941Parser.ParseBoolean(data, ref index, out var result));
            Assert.Equal(value, result);
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
            Assert.NotNull(Rfc8941Parser.ParseBoolean(data, ref index, out _));
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
            Assert.Null(Rfc8941Parser.ParseNumber(data, ref index, out var result));
            Assert.Equal(value, result);
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
            Assert.NotNull(Rfc8941Parser.ParseNumber(data, ref index, out _));
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
            Assert.Null(Rfc8941Parser.ParseString(data, ref index, out var result));
            Assert.Equal(value, result);
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
            Assert.NotNull(Rfc8941Parser.ParseString(data, ref index, out _));
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("key1", 0, "key1", 4)]
        [InlineData("keyZ1", 0, "key", 3)]
        [InlineData("TeST*key-super.value*star=0", 4, "*key-super.value*star", 25)]
        public void ParseKeyWorks(string data, int index, string value, int lastIndex)
        {
            Assert.Null(Rfc8941Parser.ParseKey(data, ref index, out var result));
            Assert.Equal(value, result);
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("some^key", 4, 4)]
        [InlineData("TEST", 0, 0)]
        [InlineData(" TEST", 0, 0)]
        [InlineData("\"TEST\"", 0, 0)]
        [InlineData("!", 0, 0)]
        [InlineData("7 Test", 0, 0)]
        [InlineData("1T0ken", 2, 2)]
        [InlineData("1T0ken", 1, 1)]
        [InlineData("1key", 0, 0)]
        [InlineData("r!dsX9AFUH162", 5, 5)]
        public void ParseKeyFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.NotNull(Rfc8941Parser.ParseKey(data, ref index, out _));
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("TOK/417", 0, "TOK/417", 7)]
        [InlineData("*Test-Token.", 0, "*Test-Token.", 12)]
        [InlineData("Test:T0ken\tWithTabs", 0, "Test:T0ken", 10)]
        [InlineData("*!#$%^&:+-./~'`^_|~", 0, "*!#$%^&:+-./~'`^_|~", 19)]
        [InlineData("*!@#$/:%^&+-.~'`^_|~", 0, "*!", 2)]
        [InlineData("*!#$%^&+-.~'`^_@|~", 0, "*!#$%^&+-.~'`^_", 15)]
        public void ParseTokenWorks(string data, int index, string value, int lastIndex)
        {
            Assert.Null(Rfc8941Parser.ParseToken(data, ref index, out var result));
            Assert.Equal(value, result.ToString());
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
            Assert.NotNull(Rfc8941Parser.ParseToken(data, ref index, out _));
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("::2u3y7", 0, "", 2)]
        [InlineData(":YWpyOTgyMzd5czdyZXkzd3I=:", 0, "YWpyOTgyMzd5czdyZXkzd3I=", 26)]
        [InlineData("xdo721::", 6, "", 8)]
        [InlineData("xc,o2!7:XiZocTI4KiZoZlxo:", 7, "XiZocTI4KiZoZlxo", 25)]
        public void ParseByteSequenceWorks(string data, int index, string value, int lastIndex)
        {
            Assert.Null(Rfc8941Parser.ParseByteSequence(data, ref index, out var result));
            Assert.Equal(value, Convert.ToBase64String(result.ToArray()));
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
            Assert.NotNull(Rfc8941Parser.ParseByteSequence(data, ref index, out _));
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, "{}", 0)]
        [InlineData(";key", 0, "{key: true}", 4)]
        [InlineData("qqcxc;  some;value=18.13;   v=\"99\";mm=*test/71 st", 5, "{some: true, value: 18.13, v: '99', mm: '*test/71'}", 46)]
        public void ParseParametersWorks(string data, int index, string value, int lastIndex)
        {
            var parsedValue = JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
            Assert.Null(Rfc8941Parser.ParseParameters(data, ref index, out var result));
            Assert.Equal(result, parsedValue); // Exchanged expected and actual to fix string.Equals(Token) != Token.Equals(string) issue
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData(";", 0, 1)]
        [InlineData("some;;*key", 4, 5)]
        [InlineData(";TEST", 0, 1)]
        [InlineData("; TEST", 0, 2)]
        [InlineData(";\"TEST\"", 0, 1)]
        [InlineData(";!", 0, 1)]
        [InlineData(";7 Test", 0, 1)]
        [InlineData("1T;0ken", 2, 3)]
        [InlineData("1;T0ken", 1, 2)]
        [InlineData(";1key", 0, 1)]
        [InlineData("r!dsX;9AFUH162", 5, 6)]
        [InlineData("qqcxc;  some;value=!18.13;   v=\"99\";mm=*test/71 st", 5, 19)]
        public void ParseParametersFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.NotNull(Rfc8941Parser.ParseParameters(data, ref index, out _));
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("!!\"Chromium\";v=\"86\", \"\"Not\\A;Brand\";v=\"99\", \"Google Chrome\";v=\"86\"", 2, "Chromium", "{v: '86'}", 19)]
        public void ParseItemWorks(string data, int index, object item, string parameters, int lastIndex)
        {
            var parsedParameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters);
            Assert.Null(Rfc8941Parser.ParseItem(data, ref index, out var result));
            Assert.Equal(item, result.Item);
            Assert.Equal(parsedParameters, result.Parameters);
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("\"Data\";v=17!", 6, 6)]
        public void ParseItemFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.NotNull(Rfc8941Parser.ParseItem(data, ref index, out _));
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("!!dx();v=86;q=\"asd\"", 4, "[]", "{v: 86, q: 'asd'}", 19)]
        [InlineData("(1;sdd=73.1;q=64 *dedx)", 0, "[{item: 1, parameters: {sdd: 73.1, q: 64}}, {item: '*dedx', parameters: {}}]", "{}", 23)]
        public void ParseInnerListWorks(string data, int index, string list, string parameters, int lastIndex)
        {
            var parsedList = JsonConvert.DeserializeObject<ParsedItem[]>(list)!;
            var parsedParameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters)!;
            Assert.Null(Rfc8941Parser.ParseInnerList(data, ref index, out var result));
            Assert.Equal(parsedList, (IReadOnlyList<ParsedItem>)result.Item, ReverseEqualityComparer<ParsedItem>.Instance);
            Assert.Equal(parsedParameters, result.Parameters);
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("  ", 1, 1)]
        [InlineData(" (", 1, 2)]
        [InlineData("  ( abc", 2, 7)]
        [InlineData(" ( some", 1, 7)]
        public void ParseInnerListFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.NotNull(Rfc8941Parser.ParseInnerList(data, ref index, out _));
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("!!\"Chromium\";v=\"86\", \"\"Not\\A;Brand\";v=\"99\", \"Google Chrome\";v=\"86\"", 2, "[{item: 'Chromium', parameters: {v: '86'}}]", "{}", 19)]
        [InlineData("!!dx();v=86;q=\"asd\"", 4, "[]", "{v: 86, q: 'asd'}", 19)]
        [InlineData("(1;sdd=73.1;q=64 *dedx)", 0, "[{item: 1, parameters: {sdd: 73.1, q: 64}}, {item: '*dedx', parameters: {}}]", "{}", 23)]
        public void ParseItemOrInnerListWorks(string data, int index, string list, string parameters, int lastIndex)
        {
            var parsedList = JsonConvert.DeserializeObject<ParsedItem[]>(list)!;
            var parsedParameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters)!;
            Assert.Null(Rfc8941Parser.ParseItemOrInnerList(data, ref index, out var result));
            if (result.Item is IReadOnlyList<ParsedItem> { } resultList)
            {
                Assert.Equal(parsedList, resultList, ReverseEqualityComparer<ParsedItem>.Instance);
                Assert.Equal(parsedParameters, result.Parameters);
            }
            else
            {
                Assert.Equal(Assert.Single(parsedList), result, ReverseEqualityComparer<ParsedItem>.Instance);
                Assert.Empty(parsedParameters);
            }

            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [InlineData("", 0, 0)]
        [InlineData("  ", 1, 2)]
        [InlineData(" (", 1, 2)]
        [InlineData("  ( abc", 2, 7)]
        [InlineData(" ( some", 1, 7)]
        public void ParseItemOrInnerListFailsCorrectly(string data, int index, int lastIndex)
        {
            Assert.NotNull(Rfc8941Parser.ParseItemOrInnerList(data, ref index, out _));
            Assert.Equal(lastIndex, index);
        }

        [Theory]
        [WhatWgTestsData("examples.json")]
        public void WhatWgTestsPass(WhatWgTestCase testCase)
        {
            ParseError? error;
            JArray actual;
            try
            {
                error = Parse(testCase.HeaderType, testCase.Header, out actual);
            }
            catch (Exception exception)
            {
                throw new TestFailedException("Failed to parse.", exception);
            }

            if (testCase.MustFail)
            {
                try
                {
                    Assert.NotNull(error);
                }
                catch (NotNullException exception)
                {
                    throw new TestFailedException("Successful must-fail test.", exception);
                }

                return;
            }
            else
            {
                if (testCase.CanFail)
                {
                    if (error is object)
                    {
                        return;
                    }
                }
                else
                {
                    try
                    {
                        Assert.Null(error);
                    }
                    catch (NullException exception)
                    {
                        throw new TestFailedException($"Unexpected parse error:\n{error}", exception);
                    }
                }
            }

            var buffer = new StringBuilder();
            using var stringWriter = new StringWriter(buffer);
            using (var writer = new JsonTextWriter(stringWriter) { CloseOutput = false })
            {
                (testCase.Expected ?? throw new InvalidDataException("Missing expected data for non-failing test.")).WriteTo(writer);
            }

            stringWriter.Flush();
            var expectedString = buffer.ToString();

            buffer.Clear();
            using (var writer = new JsonTextWriter(stringWriter) { CloseOutput = false })
            {
                actual.WriteTo(writer);
            }

            stringWriter.Flush();
            var actualString = buffer.ToString();

            try
            {
                Assert.Equal(expectedString, actualString);
            }
            catch (EqualException exception)
            {
                throw new TestFailedException("Mismatching result.", exception);
            }
        }

        private static ParseError? Parse(HeaderType headerType, string header, out JArray actual)
        {
            ParseError? error;
            var index = 0;
            switch (headerType)
            {
                case HeaderType.Item:
                    error = Rfc8941Parser.ParseItem(header, ref index, out var parsedItem);
                    actual = ConvertItem(parsedItem);
                    break;

                case HeaderType.List:
                    error = Rfc8941Parser.ParseList(header, ref index, out var parsedList);
                    actual = ConvertList(parsedList);
                    break;

                case HeaderType.Dictionary:
                    error = Rfc8941Parser.ParseDictionary(header, ref index, out var parsedDictionary);
                    actual = ConvertDictionary(parsedDictionary);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(headerType), headerType, "Unsupported header type.");
            }

            return error;

            static object ConvertValue(object value) => value switch
            {
                Token token => new JObject { ["__type"] = "token", ["value"] = token.ToString() },
                byte[] binary => new JObject { ["__type"] = "binary", ["value"] = StringEncoder.ToBase32String(binary) },
                IReadOnlyList<ParsedItem> list => ConvertList(list),
                object other => other,
            };

            static JArray ConvertItem(in ParsedItem item) => new(
                ConvertValue(item.Item),
                new JArray(item.Parameters.Select(p => new JArray(p.Key, ConvertValue(p.Value)))));

            static JArray ConvertList(IReadOnlyList<ParsedItem> list) => new(list.Select(i => ConvertItem(i)));

            static JArray ConvertDictionary(IReadOnlyDictionary<string, ParsedItem> dictionary) => new(dictionary.Select(p => new JArray(p.Key, ConvertItem(p.Value))));
        }
    }
}
