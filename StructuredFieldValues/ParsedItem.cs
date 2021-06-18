using System;
using System.Collections.Generic;

namespace StructuredFieldValues
{
    /// <summary>
    ///     Represents a parsed item, that is -- a pair of value and any additional parameters.
    /// </summary>
    ///
    /// <remarks>The valid <see cref="Value"/> and values of <see cref="Parameters"/> can be one of:
    ///     <see cref="bool"/> (Boolean),
    ///     <see cref="long"/> (Integer),
    ///     <see cref="double"/> (Decimal),
    ///     <see cref="string"/> (String),
    ///     <see cref="Token"/> (Token),
    ///     <see cref="ReadOnlyMemory{byte}"/> (Byte sequence).</remarks>
    public readonly struct ParsedItem : IEquatable<ParsedItem>
    {
        private readonly object? _value;
        private readonly IReadOnlyDictionary<string, object>? _parameters;

        public ParsedItem(object? value, IReadOnlyDictionary<string, object>? parameters)
        {
            _value = value;
            _parameters = parameters;
        }

        public object Value => _value ?? CommonValues.Empty;

        public IReadOnlyDictionary<string, object> Parameters => _parameters ?? CommonValues.EmptyParameters;

        public static bool operator ==(ParsedItem left, ParsedItem right) => left.Equals(right);

        public static bool operator !=(ParsedItem left, ParsedItem right) => !(left == right);

        public bool Equals(ParsedItem other)
            => Value.Equals(other.Value)
            && CommonValues.ParametersComparer.Equals(Parameters, other.Parameters);

        public override bool Equals(object? obj) => obj is ParsedItem item && Equals(item);

        public override int GetHashCode() => Value.GetHashCode();

        public override string? ToString() => _value?.ToString();
    }
}
