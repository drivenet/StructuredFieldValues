using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StructuredFieldValues
{
    public static class Rfc8941Parser
    {
        private static readonly object True = true;

        public static ParseError? ParseBareItem(ReadOnlySpan<char> source, ref int index, out object result)
        {
            CheckIndex(index);
            index = SkipSP(source, index);

            if (index == source.Length)
            {
                result = default(ParsedItem).Item;
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
                    {
                        if (ParseNumber(source, ref index, out var parsed) is not { } error)
                        {
                            var integer = (long)parsed;
                            if (parsed == integer)
                            {
                                result = integer;
                                return null;
                            }

                            result = parsed;
                            return null;
                        }
                        else
                        {
                            result = CommonValues.Empty;
                            return error;
                        }
                    }

                case '"':
                    {
                        if (ParseString(source, ref index, out var parsed) is not { } error)
                        {
                            result = parsed;
                            return null;
                        }
                        else
                        {
                            result = CommonValues.Empty;
                            return error;
                        }
                    }

                case '*':
                    {
                        if (ParseToken(source, ref index, out var parsed) is not { } error)
                        {
                            result = parsed;
                            return null;
                        }
                        else
                        {
                            result = CommonValues.Empty;
                            return error;
                        }
                    }

                case ':':
                    {
                        if (ParseByteSequence(source, ref index, out var parsed) is not { } error)
                        {
                            result = parsed;
                            return null;
                        }
                        else
                        {
                            result = CommonValues.Empty;
                            return error;
                        }
                    }

                case '?':
                    {
                        if (ParseBoolean(source, ref index, out var parsed) is not { } error)
                        {
                            result = parsed;
                            return null;
                        }
                        else
                        {
                            result = CommonValues.Empty;
                            return error;
                        }
                    }

                default:
                    {
                        // Rare case for Tokens, placing all these cases in switch would be inconvenient
                        if (discriminator is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))
                        {
                            if (ParseToken(source, ref index, out var parsed) is not { } error)
                            {
                                result = parsed;
                                return null;
                            }
                            else
                            {
                                result = CommonValues.Empty;
                                return error;
                            }
                        }

                        result = CommonValues.Empty;
                        return new(index, "invalid discriminator");
                    }
            }
        }

        public static ParseError? ParseItemOrInnerList(ReadOnlySpan<char> source, ref int index, out ParsedItem result)
        {
            if (index >= 0
                && index < source.Length
                && source[index] == '(')
            {
                return ParseInnerList(source, ref index, out result);
            }

            return ParseItem(source, ref index, out result);
        }

        public static ParseError? ParseInnerList(ReadOnlySpan<char> source, ref int index, out ParsedItem result)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                result = default;
                return new(index, "insufficient characters for inner list");
            }

            if (source[index] != '(')
            {
                result = default;
                return new(index, "missing opening parentheses");
            }

            ++index;

            if (spanLength - index < 1)
            {
                result = default;
                return new(index, "insufficient characters for inner list value");
            }

            var initialIndex = index;
            var localIndex = initialIndex;
            List<ParsedItem>? buffer = null;
            while (localIndex < spanLength)
            {
                localIndex = SkipSP(source, localIndex);
                if (source[localIndex] == ')')
                {
                    ++localIndex;
                    if (ParseParameters(source, ref localIndex, out var listParameters) is { } parametersError)
                    {
                        index = localIndex;
                        result = default;
                        return parametersError;
                    }

                    index = localIndex;
                    result = new(buffer, listParameters);
                    return null;
                }

                if (ParseItem(source, ref localIndex, out var item) is { } itemError)
                {
                    index = localIndex;
                    result = default;
                    return itemError;
                }

                (buffer ??= new()).Add(item);
            }

            index = localIndex;
            result = default;
            return new(localIndex, "missing closing parentheses");
        }

        public static ParseError? ParseItem(ReadOnlySpan<char> source, ref int index, out ParsedItem result)
        {
            if (ParseBareItem(source, ref index, out var item) is { } itemError)
            {
                result = default;
                return itemError;
            }

            if (ParseParameters(source, ref index, out var parameters) is { } parametersError)
            {
                result = default;
                return parametersError;
            }

            result = new(item, parameters);
            return null;
        }

        public static ParseError? ParseParameters(ReadOnlySpan<char> source, ref int index, out IReadOnlyDictionary<string, object> result)
        {
            CheckIndex(index);
            Dictionary<string, object>? parameters = null;
            var spanLength = source.Length;
            var localIndex = index;
            while (localIndex < spanLength)
            {
                if (source[localIndex] != ';')
                {
                    break;
                }

                localIndex = SkipSP(source, localIndex + 1);
                if (ParseKey(source, ref localIndex, out var key) is { } keyError)
                {
                    index = localIndex;
                    result = CommonValues.EmptyParameters;
                    return keyError;
                }

                object value;
                if (localIndex < spanLength && source[localIndex] == '=')
                {
                    ++localIndex;
                    if (ParseBareItem(source, ref localIndex, out value) is { } valueError)
                    {
                        index = localIndex;
                        result = CommonValues.EmptyParameters;
                        return valueError;
                    }
                }
                else
                {
                    value = True;
                }

                (parameters ??= new())[key] = value;
            }

            index = localIndex;
            result = parameters ?? CommonValues.EmptyParameters;
            return null;
        }

        public static ParseError? ParseKey(ReadOnlySpan<char> source, ref int index, out string result)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                result = "";
                return new(index, "insufficient characters for key");
            }

            var character = source[index];
            if (character is not ((>= 'a' and <= 'z') or '*'))
            {
                result = "";
                return new(index, "invalid leading key character");
            }

            var initialIndex = index;
            var localIndex = ++index;
            while (localIndex < spanLength)
            {
                character = source[localIndex];
                if (character is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-' or '.' or '*'))
                {
                    break;
                }

                ++localIndex;
            }

            var slice = source.Slice(initialIndex, localIndex - initialIndex);
#if NET5_0_OR_GREATER
            result = new(slice);
#else
            result = new(slice.ToArray());
#endif
            index = localIndex;
            return null;
        }

        public static ParseError? ParseBoolean(ReadOnlySpan<char> source, ref int index, out bool result)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                result = default;
                return new(index, "insufficient characters for boolean");
            }

            var discriminator = source[index];
            if (discriminator != '?')
            {
                result = default;
                return new(index, "unexpected boolean discriminator");
            }

            ++index;
            if (spanLength - index < 1)
            {
                result = default;
                return new(index, "insufficient characters for boolean value");
            }

            var value = source[index];
            switch (value)
            {
                case '0':
                    ++index;
                    result = false;
                    return null;

                case '1':
                    ++index;
                    result = true;
                    return null;

                default:
                    result = default;
                    return new(index, "unexpected boolean value");
            }
        }

        public static ParseError? ParseNumber(ReadOnlySpan<char> source, ref int index, out double result)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                result = default;
                return new(index, "insufficient characters for number");
            }

            var isNegative = source[index] == '-';
            if (isNegative)
            {
                ++index;
            }

            var separatorIndex = -1;
            var initialIndex = index;
            var localIndex = initialIndex;
            while (localIndex < spanLength)
            {
                var character = source[localIndex];
                var earlyBreak = false;
                var length = localIndex - initialIndex;
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
                            index = localIndex;
                            result = default;
                            return new(initialIndex, "misplaced decimal '.'");
                        }

                        if (length > 12)
                        {
                            index = localIndex;
                            result = default;
                            return new(localIndex, "integral part of decimal is too long", character);
                        }

                        separatorIndex = localIndex;
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
                        index = localIndex;
                        result = default;
                        return new(localIndex, "integer is too long ({0})", length);
                    }
                }
                else
                {
                    if (length > 16)
                    {
                        index = localIndex;
                        result = default;
                        return new(localIndex, "decimal is too long ({0})", length);
                    }

                    var fractionLength = localIndex - separatorIndex;
                    if (fractionLength > 3)
                    {
                        index = localIndex;
                        result = default;
                        return new(initialIndex, "decimal fraction is too long ({0})", fractionLength);
                    }
                }

                ++localIndex;
            }

            index = localIndex;
            if (index == initialIndex)
            {
                result = default;
                return new(index, "insufficient digits for number");
            }

            double value;
            if (separatorIndex < 0)
            {
                var parsed = 0L;
                for (var i = initialIndex; i < localIndex; i++)
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
                    result = default;
                    return new(initialIndex, "failed to parse decimal");
                }
            }

            if (isNegative)
            {
                value = -value;
            }

            result = value;
            return null;
        }

        public static ParseError? ParseString(ReadOnlySpan<char> source, ref int index, out string result)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                result = "";
                return new(index, "insufficient characters for string");
            }

            if (source[index] != '"')
            {
                result = "";
                return new(index, "missing opening double quote");
            }

            ++index;

            if (spanLength - index < 1)
            {
                result = "";
                return new(index, "insufficient characters for string value");
            }

            var initialIndex = index;
            var localIndex = initialIndex;
            StringBuilder? buffer = null;
            while (localIndex < spanLength)
            {
                var character = source[localIndex];
                switch (character)
                {
                    case '\\':
                        ++localIndex;
                        if (localIndex >= spanLength)
                        {
                            index = localIndex;
                            result = "";
                            return new(localIndex, "missing escaped character");
                        }

                        character = source[localIndex];
                        switch (character)
                        {
                            case '\\':
                            case '"':
                                if (buffer is null)
                                {
                                    buffer = new StringBuilder(spanLength - 2);
                                    var slice = source.Slice(initialIndex, localIndex - initialIndex - 1);
#if NET5_0_OR_GREATER
                                    buffer.Append(slice);
#else
                                    buffer.Append(slice.ToArray());
#endif
                                }

                                buffer.Append(character);
                                break;

                            default:
                                index = localIndex;
                                result = "";
                                return new(localIndex, "invalid escaped character");
                        }

                        break;

                    case '"':
                        if (buffer is object)
                        {
                            result = buffer.ToString();
                        }
                        else
                        {
                            var slice = source.Slice(initialIndex, localIndex - initialIndex);
#if NET5_0_OR_GREATER
                            result = new(slice);
#else
                            result = new(slice.ToArray());
#endif
                        }

                        index = localIndex + 1;
                        return null;

                    default:
                        if (character is < (char)0x1F or > (char)0x7F)
                        {
                            index = localIndex;
                            result = "";
                            return new(localIndex, "string character is out of range");
                        }

                        buffer?.Append(character);
                        break;
                }

                ++localIndex;
            }

            index = localIndex;
            result = "";
            return new(localIndex, "missing closing double quote");
        }

        public static ParseError? ParseToken(ReadOnlySpan<char> source, ref int index, out Token result)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 1)
            {
                result = Token.Empty;
                return new(index, "insufficient characters for token");
            }

            var character = source[index];
            if (character is not ((>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '*'))
            {
                result = Token.Empty;
                return new(index, "invalid leading token character");
            }

            var initialIndex = index;
            var localIndex = ++index;
            while (localIndex < spanLength)
            {
                character = source[localIndex];
                if (character is not ((>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9')
                    or '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-' or '.' or '^' or '_' or '`' or '|' or '~' or ':' or '/'))
                {
                    break;
                }

                ++localIndex;
            }

            var slice = source.Slice(initialIndex, localIndex - initialIndex);
#if NET5_0_OR_GREATER
            var value = new string(slice);
#else
            var value = new string(slice.ToArray());
#endif
            index = localIndex;
            result = new(value);
            return null;
        }

        public static ParseError? ParseByteSequence(ReadOnlySpan<char> source, ref int index, out ReadOnlyMemory<byte> result)
        {
            CheckIndex(index);
            var spanLength = source.Length;
            if (spanLength - index < 2)
            {
                result = Array.Empty<byte>();
                return new(index, "insufficient characters for byte sequence");
            }

            if (source[index] != ':')
            {
                result = Array.Empty<byte>();
                return new(index, "invalid opening byte sequence character");
            }

            var initialIndex = ++index;
            var slice = source.Slice(initialIndex);
            var length = slice.IndexOf(':');
            if (length < 0)
            {
                index += spanLength - 1;
                result = Array.Empty<byte>();
                return new(index, "missing closing byte sequence character");
            }

#if NET5_0_OR_GREATER
            var expectedLength = length * 3 / 4;
            var buffer = new byte[expectedLength];
            if (!Convert.TryFromBase64Chars(slice.Slice(0, length), buffer, out var written))
            {
                result = Array.Empty<byte>();
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
                result = Array.Empty<byte>();
                return new(index, "invalid byte sequence encoding");
            }
#endif

            index += length + 1;
            return null;
        }

#if !NET5_0_OR_GREATER
        public static ParseError? ParseBareItem(string source, ref int index, out object result) => ParseBareItem(source.AsSpan(), ref index, out result);

        public static ParseError? ParseItemOrInnerList(string source, ref int index, out ParsedItem result) => ParseItemOrInnerList(source.AsSpan(), ref index, out result);

        public static ParseError? ParseInnerList(string source, ref int index, out ParsedItem result) => ParseInnerList(source.AsSpan(), ref index, out result);

        public static ParseError? ParseItem(string source, ref int index, out ParsedItem result) => ParseItem(source.AsSpan(), ref index, out result);

        public static ParseError? ParseParameters(string source, ref int index, out IReadOnlyDictionary<string, object> result) => ParseParameters(source.AsSpan(), ref index, out result);

        public static ParseError? ParseKey(string source, ref int index, out string result) => ParseKey(source.AsSpan(), ref index, out result);

        public static ParseError? ParseBoolean(string source, ref int index, out bool result) => ParseBoolean(source.AsSpan(), ref index, out result);

        public static ParseError? ParseNumber(string source, ref int index, out double result) => ParseNumber(source.AsSpan(), ref index, out result);

        public static ParseError? ParseString(string source, ref int index, out string result) => ParseString(source.AsSpan(), ref index, out result);

        public static ParseError? ParseToken(string source, ref int index, out Token result) => ParseToken(source.AsSpan(), ref index, out result);

        public static ParseError? ParseByteSequence(string source, ref int index, out ReadOnlyMemory<byte> result) => ParseByteSequence(source.AsSpan(), ref index, out result);
#endif

        private static int SkipSP(ReadOnlySpan<char> source, int index)
        {
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
                throw new ArgumentOutOfRangeException(nameof(index), "Negative index.");
            }
        }
    }
}
