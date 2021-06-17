using System;
using System.Collections.Generic;

namespace StructuredFieldValues
{
    public readonly struct ParsedItem : IEquatable<ParsedItem>
    {
        private readonly object? _item;
        private readonly IReadOnlyDictionary<string, object>? _parameters;

        public ParsedItem(object? item, IReadOnlyDictionary<string, object>? parameters)
        {
            _item = item;
            _parameters = parameters;
        }

        public object Item => _item ?? CommonValues.Empty;

        public IReadOnlyDictionary<string, object> Parameters => _parameters ?? CommonValues.EmptyParameters;

        public static bool operator ==(ParsedItem left, ParsedItem right) => left.Equals(right);

        public static bool operator !=(ParsedItem left, ParsedItem right) => !(left == right);

        public bool Equals(ParsedItem other)
            => Item.Equals(other.Item)
            && CommonValues.ParametersComparer.Equals(Parameters, other.Parameters);

        public override bool Equals(object? obj) => obj is ParsedItem item && Equals(item);

        public override int GetHashCode() => Item.GetHashCode();

        public override string? ToString() => _item?.ToString();
    }
}
