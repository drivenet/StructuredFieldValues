using System;
using System.Collections.Generic;

namespace StructuredFieldValues
{
    internal static class CommonValues
    {
        public static readonly IReadOnlyList<ParsedItem> Empty = Array.Empty<ParsedItem>();
        public static readonly IReadOnlyDictionary<string, object> EmptyParameters = new Dictionary<string, object>();
        public static readonly DictionaryEqualityComparer<string, object> ParametersComparer = new();
    }
}
