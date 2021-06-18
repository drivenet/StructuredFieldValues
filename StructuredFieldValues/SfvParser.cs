using System;
using System.Collections.Generic;

namespace StructuredFieldValues
{
    /// <summary>
    ///     This is a structured field value parser that respects RFC 8941.
    /// </summary>
    ///
    /// <remarks>For implementation details check out <see cref="Rfc8941Parser"/>.</remarks>
    public static class SfvParser
    {
        public static ParseError? ParseItem(ReadOnlySpan<char> source, ref int index, out ParsedItem value)
            => Rfc8941Parser.ParseItemField(source, ref index, out value);

        public static ParseError? ParseItem(string source, ref int index, out ParsedItem value)
            => Rfc8941Parser.ParseItemField(source, ref index, out value);

        public static ParseError? ParseList(ReadOnlySpan<char> source, ref int index, out IReadOnlyList<ParsedItem> value)
            => Rfc8941Parser.ParseListField(source, ref index, out value);

        public static ParseError? ParseList(string source, ref int index, out IReadOnlyList<ParsedItem> value)
            => Rfc8941Parser.ParseListField(source, ref index, out value);

        public static ParseError? ParseDictionary(ReadOnlySpan<char> source, ref int index, out IReadOnlyDictionary<string, ParsedItem> value)
            => Rfc8941Parser.ParseDictionaryField(source, ref index, out value);

        public static ParseError? ParseDictionary(string source, ref int index, out IReadOnlyDictionary<string, ParsedItem> value)
            => Rfc8941Parser.ParseDictionaryField(source, ref index, out value);
    }
}
