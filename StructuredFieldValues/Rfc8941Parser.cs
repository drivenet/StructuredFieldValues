using System;
using System.Globalization;
using System.Text;

namespace StructuredFieldValues
{
    public static class Rfc8941Parser
    {
        public static ParseResult<object> ParseBareItem(ReadOnlySpan<char> source, int index = 0)
        {
            index = SkipSP(source, index);

            if (index == source.Length)
            {
                return new(index, "empty bare item");
            }

            var discriminator = source[index];
            switch (discriminator)
            {
                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return ParseNumber(source, ref index).Box();

                case '"':
                    return ParseString(source, ref index).Box();

                case '*':
                    return ParseToken(source, ref index).Box();

                case ':':
                    return ParseByteSequence(source, ref index).Box();

                case '?':
                    return ParseBoolean(source, ref index).Box();

                default:
                    // Rare case for Tokens, placing all these cases in switch would be inconvenient
                    if (discriminator is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))
                    {
                        return ParseToken(source, ref index).Box();
                    }

                    return new(index, "invalid discriminator");
            }
        }

        public static ParseResult<bool> ParseBoolean(ReadOnlySpan<char> source, ref int index)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                return new(index, "insufficient characters for boolean");
            }

            var discriminator = source[index];
            if (discriminator != '?')
            {
                return new(index, "unexpected boolean discriminator");
            }

            ++index;
            if (spanLength - index < 1)
            {
                return new(index, "insufficient characters for boolean value");
            }

            var value = source[index];
            switch (value)
            {
                case '0':
                    ++index;
                    return new(false);

                case '1':
                    ++index;
                    return new(true);

                default:
                    return new(index, "unexpected boolean value");
            };
        }

        public static ParseResult<double> ParseNumber(ReadOnlySpan<char> source, ref int index)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                return new(index, "insufficient characters for number");
            }

            var isNegative = source[index] == '-';
            if (isNegative)
            {
                ++index;
            }

            var separatorIndex = -1;
            var initialIndex = index;

            while (index < spanLength)
            {
                var character = source[index];
                var earlyBreak = false;
                var length = index - initialIndex;
                switch (character)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        break;

                    case '.':
                        if (separatorIndex >= 0)
                        {
                            return new(initialIndex, "misplaced decimal '.'");
                        }

                        if (length > 12)
                        {
                            return new(index, "integral part of decimal is too long", character);
                        }

                        separatorIndex = index;
                        break;

                    default:
                        earlyBreak = true;
                        break;
                }

                if (earlyBreak)
                {
                    break;
                }

                if (separatorIndex < 0)
                {
                    if (length > 15)
                    {
                        return new(index, "integer is too long ({0})", length);
                    }
                }
                else
                {
                    if (length > 16)
                    {
                        return new(index, "decimal is too long ({0})", length);
                    }

                    var fractionLength = index - separatorIndex;
                    if (fractionLength > 3)
                    {
                        return new(initialIndex, "decimal fraction is too long ({0})", fractionLength);
                    }
                }

                ++index;
            }

            if (index == initialIndex)
            {
                return new(index, "insufficient digits for number");
            }

            double value;
            if (separatorIndex < 0)
            {
                var parsed = 0L;
                for (var i = initialIndex; i < index; i++)
                {
                    parsed *= 10;
                    parsed += source[i] - '0';
                }

                value = parsed;
            }
            else
            {
                var slice = source.Slice(initialIndex, index - initialIndex);
#if NET5_0_OR_GREATER
                if (!double.TryParse(slice, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
#else
                var valueString = new string(slice.ToArray());
                if (!double.TryParse(valueString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
#endif
                {
                    return new(initialIndex, "failed to parse decimal");
                }
            }

            if (isNegative)
            {
                value = -value;
            }

            return new(value);
        }

        public static ParseResult<string> ParseString(ReadOnlySpan<char> source, ref int index)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                return new(index, "insufficient characters for string");
            }

            if (source[index] != '"')
            {
                return new(index, "missing opening double quote");
            }

            ++index;

            if (spanLength - index < 1)
            {
                return new(index, "insufficient characters for string value");
            }

            var initialIndex = index;

            StringBuilder? buffer = null;
            while (index < spanLength)
            {
                var character = source[index];
                switch (character)
                {
                    case '\\':
                        ++index;
                        if (index >= spanLength)
                        {
                            return new(index, "missing escaped character");
                        }

                        character = source[index];
                        switch (character)
                        {
                            case '\\':
                            case '"':
                                if (buffer is null)
                                {
                                    buffer = new StringBuilder(spanLength - 2);
                                    var slice = source.Slice(initialIndex, index - initialIndex - 1);
#if NET5_0_OR_GREATER
                                    buffer.Append(slice);
#else
                                    buffer.Append(slice.ToArray());
#endif
                                }

                                buffer.Append(character);
                                break;

                            default:
                                return new(index, "invalid escaped character");
                        }

                        break;

                    case '"':
                        string result;
                        if (buffer is object)
                        {
                            result = buffer.ToString();
                        }
                        else
                        {
                            var slice = source.Slice(initialIndex, index - initialIndex);
#if NET5_0_OR_GREATER
                            result = new string(slice);
#else
                            result = new string(slice.ToArray());
#endif
                        }

                        ++index;
                        return new(result);

                    default:
                        if (character is < (char)0x1F or > (char)0x7F)
                        {
                            return new(index, "string character is out of range");
                        }

                        buffer?.Append(character);
                        break;
                }

                ++index;
            }

            return new(index, "missing closing double quote");
        }

        public static ParseResult<string> ParseToken(ReadOnlySpan<char> source, ref int index)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                return new(index, "insufficient characters for token");
            }

            var character = source[index];
            if (character is not ((>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '*'))
            {
                return new(index, "invalid leading token character");
            }

            var initialIndex = index++;
            while (index < spanLength)
            {
                character = source[index];
                if (character is not ((>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9')
                    or '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-' or '.' or '^' or '_' or '`' or '|' or '~'))
                {
                    break;
                }

                ++index;
            }

            var slice = source.Slice(initialIndex, index - initialIndex);
#if NET5_0_OR_GREATER
            var result = new string(slice);
#else
            var result = new string(slice.ToArray());
#endif
            return new(result);
        }

        public static ParseResult<ReadOnlyMemory<byte>> ParseByteSequence(ReadOnlySpan<char> source, ref int index)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 2)
            {
                return new(index, "insufficient characters for byte sequence");
            }

            if (source[index] != ':')
            {
                return new(index, "invalid opening byte sequence character");
            }

            var initialIndex = ++index;
            var slice = source.Slice(initialIndex);
            var length = slice.IndexOf(':');
            if (length < 0)
            {
                index += spanLength - 1;
                return new(index, "missing closing byte sequence character");
            }

            ReadOnlyMemory<byte> result;
#if NET5_0_OR_GREATER
            var expectedLength = length * 3 / 4;
            var buffer = new byte[expectedLength];
            if (!Convert.TryFromBase64Chars(slice.Slice(0, length), buffer, out var written))
            {
                return new(index, "invalid byte sequence encoding");
            }

            result = buffer.AsMemory(0, written);
#else
            try
            {
                result = Convert.FromBase64CharArray(slice.Slice(0, length).ToArray(), 0, length);
            }
            catch (FormatException)
            {
                return new(index, "invalid byte sequence encoding");
            }
#endif

            index += length + 1;
            return new(result);
        }

#if !NET5_0_OR_GREATER
        public static ParseResult<object> ParseBareItem(string source) => ParseBareItem(source.AsSpan());

        public static ParseResult<bool> ParseBoolean(string source, ref int index) => ParseBoolean(source.AsSpan(), ref index);

        public static ParseResult<double> ParseNumber(string source, ref int index) => ParseNumber(source.AsSpan(), ref index);

        public static ParseResult<string> ParseString(string source, ref int index) => ParseString(source.AsSpan(), ref index);

        public static ParseResult<string> ParseToken(string source, ref int index) => ParseToken(source.AsSpan(), ref index);

        public static ParseResult<ReadOnlyMemory<byte>> ParseByteSequence(string source, ref int index) => ParseByteSequence(source.AsSpan(), ref index);
#endif

        private static int SkipSP(ReadOnlySpan<char> source, int index)
        {
            CheckIndex(index);
            while (index < source.Length && source[index] == ' ')
            {
                ++index;
            }

            return index;
        }

        private static void CheckIndex(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Negative boolean index.");
            }
        }
    }
}
