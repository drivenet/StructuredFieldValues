using System.Collections.Generic;

namespace StructuredFieldValues;

internal sealed class DictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<IReadOnlyDictionary<TKey, TValue>>
    where TKey : notnull
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

    public int GetHashCode(IReadOnlyDictionary<TKey, TValue> obj)
    {
        var count = obj.Count;
        if (count == 0)
        {
            return 0;
        }

        var minKeyHash = int.MaxValue;
        var maxKeyHash = int.MinValue;
        var minValueHash = int.MaxValue;
        var maxValueHash = int.MinValue;
        foreach (var pair in obj)
        {
            var keyHash = pair.Key.GetHashCode();
            if (keyHash < minKeyHash)
            {
                minKeyHash = keyHash;
            }
            else if (keyHash > maxKeyHash)
            {
                maxKeyHash = keyHash;
            }

            if (pair.Value is { } value)
            {
                var valueHash = value.GetHashCode();
                if (valueHash < minValueHash)
                {
                    minValueHash = valueHash;
                }
                else if (keyHash > maxValueHash)
                {
                    maxValueHash = valueHash;
                }
            }
        }

        return (minKeyHash, maxKeyHash, minValueHash, maxValueHash, -count).GetHashCode();
    }
}
