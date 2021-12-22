using System;
using System.Collections.Generic;

namespace StructuredFieldValues;

/// <summary>
///     This is a structured field value parser that respects RFC 8941.
/// </summary>
///
/// <remarks>For implementation details check out <see cref="Rfc8941Parser"/>.</remarks>
public static class SfvParser
{
    /// <summary>
    ///     Attempts to parse an item field.
    /// </summary>
    ///
    /// <param name="source">The source string.</param>
    /// <param name="value">The resulting value.</param>
    /// <returns>An error if parsing failed; elseway <c>null</c>.</returns>
    public static ParseError? ParseItem(ReadOnlySpan<char> source, out ParsedItem value)
    {
        var index = 0;
        return Rfc8941Parser.ParseItemField(source, ref index, out value);
    }

    /// <summary>
    ///     Attempts to parse an item field.
    /// </summary>
    ///
    /// <param name="source">The source string.</param>
    /// <param name="value">The resulting value.</param>
    /// <returns>An error if parsing failed; elseway <c>null</c>.</returns>
    public static ParseError? ParseItem(string source, out ParsedItem value)
    {
        var index = 0;
        return Rfc8941Parser.ParseItemField(source, ref index, out value);
    }

    /// <summary>
    ///     Attempts to parse an list field.
    /// </summary>
    ///
    /// <param name="source">The source string.</param>
    /// <param name="value">The resulting value.</param>
    /// <returns>An error if parsing failed; elseway <c>null</c>.</returns>
    public static ParseError? ParseList(ReadOnlySpan<char> source, out IReadOnlyList<ParsedItem> value)
    {
        var index = 0;
        return Rfc8941Parser.ParseListField(source, ref index, out value);
    }

    /// <summary>
    ///     Attempts to parse a list field.
    /// </summary>
    ///
    /// <param name="source">The source string.</param>
    /// <param name="value">The resulting value.</param>
    /// <returns>An error if parsing failed; elseway <c>null</c>.</returns>
    public static ParseError? ParseList(string source, out IReadOnlyList<ParsedItem> value)
    {
        var index = 0;
        return Rfc8941Parser.ParseListField(source, ref index, out value);
    }

    /// <summary>
    ///     Attempts to parse a dictionary field.
    /// </summary>
    ///
    /// <param name="source">The source string.</param>
    /// <param name="value">The resulting value.</param>
    /// <returns>An error if parsing failed; elseway <c>null</c>.</returns>
    public static ParseError? ParseDictionary(ReadOnlySpan<char> source, out IReadOnlyDictionary<string, ParsedItem> value)
    {
        var index = 0;
        return Rfc8941Parser.ParseDictionaryField(source, ref index, out value);
    }

    /// <summary>
    ///     Attempts to parse a dictionary field.
    /// </summary>
    ///
    /// <param name="source">The source string.</param>
    /// <param name="value">The resulting value.</param>
    /// <returns>An error if parsing failed; elseway <c>null</c>.</returns>
    public static ParseError? ParseDictionary(string source, out IReadOnlyDictionary<string, ParsedItem> value)
    {
        var index = 0;
        return Rfc8941Parser.ParseDictionaryField(source, ref index, out value);
    }
}
