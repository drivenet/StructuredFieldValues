using System;
using System.Globalization;

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
                case '?':
                    return ParseBoolean(source, index).Map(i => (object)i);

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
                    return ParseNumber(source, index).Map(i => (object)i);

                default:
                    return new(index, "invalid discriminator");
            }
        }

        public static ParseResult<bool> ParseBoolean(ReadOnlySpan<char> source, int index = 0)
        {
            CheckIndex(index);
            if (source.Length - index < 2)
            {
                return new(index, "insufficient characters for boolean");
            }

            var discriminator = source[index];
            if (discriminator != '?')
            {
                return new(index, "unexpected boolean discriminator");
            }

            var value = source[++index];
            return value switch
            {
                '0' => new(false),
                '1' => new(true),
                _ => new(index, "unexpected boolean value"),
            };
        }

        public static ParseResult<double> ParseNumber(ReadOnlySpan<char> source, int index = 0)
        {
            CheckIndex(index);
            if (source.Length - index < 1)
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

            while (index < source.Length)
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

#if !NET5_0_OR_GREATER
        public static ParseResult<object> ParseBareItem(string source) => ParseBareItem(source.AsSpan());

        public static ParseResult<bool> ParseBoolean(string source) => ParseBoolean(source.AsSpan());

        public static ParseResult<double> ParseNumber(string source) => ParseNumber(source.AsSpan());
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
