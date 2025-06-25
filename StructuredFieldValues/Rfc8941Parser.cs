using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StructuredFieldValues;

/// <summary>
///     This is an RFC 8941-compliant parser of structured field values for HTTP. For most parsing needs consider using the simpler <see cref="SfvParser"/>.
/// </summary>
internal static class Rfc8941Parser
{
    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly UTF8Encoding UTF8Encoding = new(false, true);

    private static readonly object True = true;

    public static ParseError? ParseItemField(ReadOnlySpan<char> source, ref int index, out ParsedItem result)
    {
        index = BeginParse(source, index);
        if (ParseItem(source, ref index, out result) is { } error)
        {
            return error;
        }

        if (EndParse(source, ref index) is { } endError)
        {
            result = default;
            return endError;
        }

        return null;
    }

    public static ParseError? ParseListField(ReadOnlySpan<char> source, ref int index, out IReadOnlyList<ParsedItem> result)
    {
        index = BeginParse(source, index);
        if (ParseList(source, ref index, out result) is { } error)
        {
            return error;
        }

        if (EndParse(source, ref index) is { } endError)
        {
            result = CommonValues.Empty;
            return endError;
        }

        return null;
    }

    public static ParseError? ParseDictionaryField(ReadOnlySpan<char> source, ref int index, out IReadOnlyDictionary<string, ParsedItem> result)
    {
        index = BeginParse(source, index);
        if (ParseDictionary(source, ref index, out result) is { } error)
        {
            return error;
        }

        if (EndParse(source, ref index) is { } endError)
        {
            result = CommonValues.EmptyDictionary;
            return endError;
        }

        return null;
    }

    public static ParseError? ParseField(FieldType fieldType, ReadOnlySpan<char> source, ref int index, out object result)
    {
        index = BeginParse(source, index);
        ParseError? error;
        switch (fieldType)
        {
            case FieldType.Item:
                error = ParseItem(source, ref index, out var item);
                result = item;
                break;

            case FieldType.List:
                error = ParseList(source, ref index, out var list);
                result = list;
                break;

            case FieldType.Dictionary:
                error = ParseDictionary(source, ref index, out var dictionary);
                result = dictionary;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Unsupported field type.");
        }

        if (error is not null)
        {
            return error;
        }

        if (EndParse(source, ref index) is { } endError)
        {
            switch (fieldType)
            {
                case FieldType.Item:
                case FieldType.List:
                    result = CommonValues.Empty;
                    break;

                case FieldType.Dictionary:
                    result = CommonValues.EmptyDictionary;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Unsupported fallback field type.");
            }

            return endError;
        }

        return null;
    }

    public static ParseError? ParseBareItem(ReadOnlySpan<char> source, ref int index, out object result)
    {
        CheckIndex(index);
        if (index == source.Length)
        {
            result = default(ParsedItem).Value;
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

            case '%':
                {
                    if (ParseDisplayString(source, ref index, out var parsed) is not { } error)
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

            case '@':
                {
                    if (ParseDate(source, ref index, out var parsed) is not { } error)
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

    public static ParseError? ParseDictionary(ReadOnlySpan<char> source, ref int index, out IReadOnlyDictionary<string, ParsedItem> result)
    {
        CheckIndex(index);
        var spanLength = source.Length;
        var localIndex = index;
        if (localIndex == spanLength)
        {
            result = CommonValues.EmptyDictionary;
            return null;
        }

        Dictionary<string, ParsedItem>? dictionary = null;
        while (true)
        {
            if (ParseKey(source, ref localIndex, out var key) is { } keyError)
            {
                index = localIndex;
                result = CommonValues.EmptyDictionary;
                return keyError;
            }

            ParsedItem value;
            if (localIndex != spanLength && source[localIndex] == '=')
            {
                ++localIndex;
                if (ParseItemOrInnerList(source, ref localIndex, out value) is { } itemError)
                {
                    index = localIndex;
                    result = CommonValues.EmptyDictionary;
                    return itemError;
                }
            }
            else
            {
                if (ParseParameters(source, ref localIndex, out var parameters) is { } itemError)
                {
                    index = localIndex;
                    result = CommonValues.EmptyDictionary;
                    return itemError;
                }

                value = new(True, parameters);
            }

            (dictionary ??= new())[key] = value;
            localIndex = SkipOWS(source, localIndex);
            if (localIndex == spanLength)
            {
                index = localIndex;
                result = dictionary;
                return null;
            }

            if (source[localIndex] != ',')
            {
                index = localIndex;
                result = CommonValues.EmptyDictionary;
                return new(index, "invalid dictionary separator");
            }

            localIndex = SkipOWS(source, localIndex + 1);
            if (localIndex == spanLength)
            {
                index = localIndex;
                result = CommonValues.EmptyDictionary;
                return new(index, "trailing comma encountered in dictionary");
            }
        }
    }

    public static ParseError? ParseList(ReadOnlySpan<char> source, ref int index, out IReadOnlyList<ParsedItem> result)
    {
        CheckIndex(index);
        var spanLength = source.Length;
        var localIndex = index;
        if (localIndex == spanLength)
        {
            result = CommonValues.Empty;
            return null;
        }

        List<ParsedItem>? list = null;
        while (true)
        {
            if (ParseItemOrInnerList(source, ref localIndex, out var item) is { } error)
            {
                index = localIndex;
                result = CommonValues.Empty;
                return error;
            }

            (list ??= new()).Add(item);
            localIndex = SkipOWS(source, localIndex);
            if (localIndex == spanLength)
            {
                index = localIndex;
                result = list;
                return null;
            }

            if (source[localIndex] != ',')
            {
                index = localIndex;
                result = CommonValues.Empty;
                return new(index, "invalid list separator");
            }

            localIndex = SkipOWS(source, localIndex + 1);
            if (localIndex == spanLength)
            {
                index = localIndex;
                result = CommonValues.Empty;
                return new(index, "trailing comma encountered in list");
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
        while (localIndex != spanLength)
        {
            var startIndex = localIndex;
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
            else if (localIndex == startIndex && buffer is not null)
            {
                index = localIndex;
                result = default;
                return new(index, "missing space separator");
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
        while (localIndex != spanLength)
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
            if (localIndex != spanLength && source[localIndex] == '=')
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
        while (localIndex != spanLength)
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

    public static ParseError? ParseDate(ReadOnlySpan<char> source, ref int index, out DateTime result)
    {
        CheckIndex(index);
        var spanLength = source.Length;
        if (spanLength - index < 1)
        {
            result = default;
            return new(index, "insufficient characters for date");
        }

        var character = source[index];
        if (character != '@')
        {
            result = default;
            return new(index, "invalid leading date character");
        }

        ++index;

        if (spanLength - index < 1)
        {
            result = default;
            return new(index, "insufficient characters for date value");
        }

        var isNegative = source[index] == '-';
        if (isNegative)
        {
            ++index;
        }

        var initialIndex = index;
        var localIndex = initialIndex;
        while (localIndex != spanLength)
        {
            character = source[localIndex];
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

                default:
                    earlyBreak = true;
                    break;
            }

            if (earlyBreak)
            {
                break;
            }

            ++length;
            if (length > 15)
            {
                index = localIndex;
                result = default;
                return new(localIndex, "date is too long ({0})", length);
            }

            ++localIndex;
        }

        index = localIndex;
        if (index == initialIndex)
        {
            result = default;
            return new(index, "insufficient digits for date");
        }

        var parsed = 0L;
        for (var i = initialIndex; i < localIndex; i++)
        {
            parsed *= 10;
            parsed += source[i] - '0';
        }

        if (isNegative)
        {
            parsed = -parsed;
        }

        result = Epoch.AddSeconds(parsed);
        return null;
    }

#pragma warning disable CA1502 // Parser code
    public static ParseError? ParseNumber(ReadOnlySpan<char> source, ref int index, out object result)
#pragma warning restore CA1502
    {
        CheckIndex(index);
        var spanLength = source.Length;
        if (spanLength - index < 1)
        {
            result = CommonValues.Empty;
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
        while (localIndex != spanLength)
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
                        result = CommonValues.Empty;
                        return new(initialIndex, "misplaced decimal '.'");
                    }

                    if (length > 12)
                    {
                        index = localIndex;
                        result = CommonValues.Empty;
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

            ++length;
            if (separatorIndex < 0)
            {
                if (length > 15)
                {
                    index = localIndex;
                    result = CommonValues.Empty;
                    return new(localIndex, "integer is too long ({0})", length);
                }
            }
            else
            {
                if (length > 16)
                {
                    index = localIndex;
                    result = CommonValues.Empty;
                    return new(localIndex, "decimal is too long ({0})", length);
                }

                var fractionLength = localIndex - separatorIndex;
                if (fractionLength > 3)
                {
                    index = localIndex;
                    result = CommonValues.Empty;
                    return new(initialIndex, "decimal fraction is too long ({0})", fractionLength);
                }
            }

            ++localIndex;
        }

        index = localIndex;
        if (index == initialIndex)
        {
            result = CommonValues.Empty;
            return new(index, "insufficient digits for number");
        }

        if (separatorIndex < 0)
        {
            var parsed = 0L;
            for (var i = initialIndex; i < localIndex; i++)
            {
                parsed *= 10;
                parsed += source[i] - '0';
            }

            if (isNegative)
            {
                parsed = -parsed;
            }

            result = parsed;
        }
        else
        {
            var slice = source.Slice(initialIndex, localIndex - initialIndex);
#if NET5_0_OR_GREATER
            if (!double.TryParse(slice, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
#else
            var valueString = new string(slice.ToArray());
            if (!double.TryParse(valueString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
#endif
            {
                result = CommonValues.Empty;
                return new(initialIndex, "failed to parse decimal");
            }

            if (isNegative)
            {
                parsed = -parsed;
            }

            result = parsed;
        }

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
        while (localIndex != spanLength)
        {
            var character = source[localIndex];
            switch (character)
            {
                case '\\':
                    ++localIndex;
                    if (localIndex == spanLength)
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
                    if (buffer is not null)
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
                    if ((int)character is not (>= 0x20 and <= 0x7E))
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

    public static ParseError? ParseDisplayString(ReadOnlySpan<char> source, ref int index, out DisplayString result)
    {
        CheckIndex(index);
        var spanLength = source.Length;
        if (spanLength - index < 1)
        {
            result = DisplayString.Empty;
            return new(index, "insufficient characters for display string");
        }

        var character = source[index];
        if (character != '%')
        {
            result = DisplayString.Empty;
            return new(index, "invalid leading display string character");
        }

        ++index;

        if (source[index] != '"')
        {
            result = DisplayString.Empty;
            return new(index, "missing opening double quote");
        }

        ++index;

        if (spanLength - index < 1)
        {
            result = DisplayString.Empty;
            return new(index, "insufficient characters for display string value");
        }

        var initialIndex = index;
        var localIndex = initialIndex;
        byte[]? buffer = null;
        var bufferLength = 0;
        try
        {
            while (localIndex != spanLength)
            {
                character = source[localIndex];
                switch (character)
                {
                    case '"':
                        string resultValue;
                        if (buffer is not null)
                        {
                            try
                            {
                                resultValue = UTF8Encoding.GetString(buffer, 0, bufferLength);
                            }
                            catch (DecoderFallbackException exception)
                            {
                                index = exception.Index;
                                result = DisplayString.Empty;
                                return new(exception.Index, "invalid UTF-8 encoding");
                            }
                        }
                        else
                        {
                            var slice = source.Slice(initialIndex, localIndex - initialIndex);
#if NET5_0_OR_GREATER
                            resultValue = new(slice);
#else
                            resultValue = new(slice.ToArray());
#endif
                        }

                        result = new(resultValue);
                        index = localIndex + 1;
                        return null;

                    case '%':
                        var pctIndex = localIndex++;
                        if (localIndex == spanLength)
                        {
                            index = pctIndex;
                            result = DisplayString.Empty;
                            return new(localIndex, "missing encoded nybble 1");
                        }

                        var nybble1 = DecodeNybble(source[localIndex]);
                        if (nybble1 < 0)
                        {
                            index = pctIndex;
                            result = DisplayString.Empty;
                            return new(localIndex, "invalid encoded nybble 1");
                        }

                        ++localIndex;
                        if (localIndex == spanLength)
                        {
                            index = pctIndex;
                            result = DisplayString.Empty;
                            return new(localIndex, "missing encoded nybble 2");
                        }

                        var nybble2 = DecodeNybble(source[localIndex]);
                        if (nybble2 < 0)
                        {
                            index = pctIndex;
                            result = DisplayString.Empty;
                            return new(localIndex, "invalid encoded nybble 2");
                        }

                        var value = unchecked((byte)((nybble1 << 4) | nybble2));
                        if (buffer is null)
                        {
                            buffer = ArrayPool<byte>.Shared.Rent((spanLength - initialIndex) * 4);

                            // At this time, only ASCII characters can be present in the array, so the conversion can be done without encoding
                            for (var i = initialIndex; i < pctIndex; i++)
                            {
                                buffer[bufferLength++] = (byte)source[i];
                            }
                        }

                        buffer[bufferLength++] = value;
                        break;

                    default:
                        if ((int)character is not (>= 0x20 and <= 0x7E))
                        {
                            index = localIndex;
                            result = DisplayString.Empty;
                            return new(localIndex, "display string character is out of range");
                        }

                        if (buffer is not null)
                        {
                            buffer[bufferLength++] = unchecked((byte)character);
                        }

                        break;
                }

                ++localIndex;
            }
        }
        finally
        {
            if (buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        index = localIndex;
        result = DisplayString.Empty;
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
        while (localIndex != spanLength)
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

        var binarySlice = slice.Slice(0, length);

        // This is required to pass strict tests
        if (binarySlice.IndexOf(' ') >= 0)
        {
            result = Array.Empty<byte>();
            return new(index, "unexpected space in byte sequence");
        }

#if NET5_0_OR_GREATER
        var expectedLength = length * 3 / 4;
        var buffer = new byte[expectedLength];
        if (!Convert.TryFromBase64Chars(binarySlice, buffer, out var written))
        {
            result = Array.Empty<byte>();
            return new(index, "invalid byte sequence encoding");
        }

        result = buffer.AsMemory(0, written);
#else
        try
        {
            result = Convert.FromBase64CharArray(binarySlice.ToArray(), 0, length);
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

    public static ParseError? ParseItemField(string source, ref int index, out ParsedItem result) => ParseItemField(source.AsSpan(), ref index, out result);

    public static ParseError? ParseListField(string source, ref int index, out IReadOnlyList<ParsedItem> result) => ParseListField(source.AsSpan(), ref index, out result);

    public static ParseError? ParseDictionaryField(string source, ref int index, out IReadOnlyDictionary<string, ParsedItem> result) => ParseDictionaryField(source.AsSpan(), ref index, out result);

    public static ParseError? ParseField(FieldType fieldType, string source, ref int index, out object result) => ParseField(fieldType, source.AsSpan(), ref index, out result);

    public static ParseError? ParseBareItem(string source, ref int index, out object result) => ParseBareItem(source.AsSpan(), ref index, out result);

    public static ParseError? ParseDictionary(string source, ref int index, out IReadOnlyDictionary<string, ParsedItem> result) => ParseDictionary(source.AsSpan(), ref index, out result);

    public static ParseError? ParseList(string source, ref int index, out IReadOnlyList<ParsedItem> result) => ParseList(source.AsSpan(), ref index, out result);

    public static ParseError? ParseItemOrInnerList(string source, ref int index, out ParsedItem result) => ParseItemOrInnerList(source.AsSpan(), ref index, out result);

    public static ParseError? ParseInnerList(string source, ref int index, out ParsedItem result) => ParseInnerList(source.AsSpan(), ref index, out result);

    public static ParseError? ParseItem(string source, ref int index, out ParsedItem result) => ParseItem(source.AsSpan(), ref index, out result);

    public static ParseError? ParseParameters(string source, ref int index, out IReadOnlyDictionary<string, object> result) => ParseParameters(source.AsSpan(), ref index, out result);

    public static ParseError? ParseKey(string source, ref int index, out string result) => ParseKey(source.AsSpan(), ref index, out result);

    public static ParseError? ParseBoolean(string source, ref int index, out bool result) => ParseBoolean(source.AsSpan(), ref index, out result);

    public static ParseError? ParseDate(string source, ref int index, out DateTime result) => ParseDate(source.AsSpan(), ref index, out result);

    public static ParseError? ParseNumber(string source, ref int index, out object result) => ParseNumber(source.AsSpan(), ref index, out result);

    public static ParseError? ParseString(string source, ref int index, out string result) => ParseString(source.AsSpan(), ref index, out result);

    public static ParseError? ParseDisplayString(string source, ref int index, out DisplayString result) => ParseDisplayString(source.AsSpan(), ref index, out result);

    public static ParseError? ParseToken(string source, ref int index, out Token result) => ParseToken(source.AsSpan(), ref index, out result);

    public static ParseError? ParseByteSequence(string source, ref int index, out ReadOnlyMemory<byte> result) => ParseByteSequence(source.AsSpan(), ref index, out result);

    private static int DecodeNybble(char character)
    {
        if (character is >= '0' and <= '9')
        {
            return character - '0';
        }

        if (character is >= 'a' and <= 'f')
        {
            return character - 'a' + 10;
        }

        return -1;
    }

    private static int SkipSP(ReadOnlySpan<char> source, int index)
    {
        while (index < source.Length && source[index] == ' ')
        {
            ++index;
        }

        return index;
    }

    private static int SkipOWS(ReadOnlySpan<char> source, int index)
    {
        while (index < source.Length
            && source[index] is ' ' or '\t')
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

    private static int BeginParse(ReadOnlySpan<char> source, int index)
    {
        CheckIndex(index);
        return SkipSP(source, index);
    }

    private static ParseError? EndParse(ReadOnlySpan<char> source, ref int index)
    {
        index = SkipSP(source, index);
        return index == source.Length ? null : new(index, "extra trailing whitespace");
    }
}
