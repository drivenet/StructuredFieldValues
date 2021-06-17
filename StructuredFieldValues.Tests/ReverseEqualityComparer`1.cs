using System.Collections.Generic;

namespace StructuredFieldValues.Tests
{
    internal sealed class ReverseEqualityComparer<T> : IEqualityComparer<T>
    {
        public static IEqualityComparer<T> Instance { get; } = new ReverseEqualityComparer<T>();

        public bool Equals(T? x, T? y)
        => y is object ? y.Equals(x!) : x is null;

        public int GetHashCode(T obj) => obj?.GetHashCode() ?? -1;
    }
}
