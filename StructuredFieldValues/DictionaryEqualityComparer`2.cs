using System.Collections.Generic;
using System.Linq;

namespace StructuredFieldValues;

internal sealed class DictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<IReadOnlyDictionary<TKey, TValue>>
{
    public bool Equals(IReadOnlyDictionary<TKey, TValue>? x, IReadOnlyDictionary<TKey, TValue>? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null
            || y is null
            || x.Count != y.Count)
        {
            return false;
        }

        foreach (var pair in x)
        {
            if (!y.TryGetValue(pair.Key, out var otherValue))
            {
                return false;
            }

            if (pair.Value is { } value)
            {
                if (!value.Equals(otherValue))
                {
                    return false;
                }
            }
            else
            {
                if (otherValue is object)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public int GetHashCode(IReadOnlyDictionary<TKey, TValue>? obj) => obj?.FirstOrDefault().GetHashCode() ?? -1;
}
