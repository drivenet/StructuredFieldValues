using System;
using System.Collections.Generic;

namespace StructuredFieldValues
{
#pragma warning disable CA1815 // Override equals and operator equals on value types -- not needed here
    public readonly struct ParsedInnerList
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        private readonly IReadOnlyList<ParsedItem>? _list;
        private readonly IReadOnlyDictionary<string, object>? _parameters;

        public ParsedInnerList(IReadOnlyList<ParsedItem>? list, IReadOnlyDictionary<string, object> parameters)
        {
            _list = list;
            _parameters = parameters;
        }

        public IReadOnlyList<ParsedItem> List => _list ?? Array.Empty<ParsedItem>();

        public IReadOnlyDictionary<string, object> Parameters => _parameters ?? CommonValues.EmptyParameters;
    }
}
