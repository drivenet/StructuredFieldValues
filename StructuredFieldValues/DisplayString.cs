using System;

namespace StructuredFieldValues;

public sealed class DisplayString : IEquatable<DisplayString>, IEquatable<string>
{
    public static readonly DisplayString Empty = new("");

    private readonly string _value;

    public DisplayString(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator string(DisplayString value) => value._value;

    public static bool operator ==(DisplayString? left, DisplayString? right) => left is not null ? left.Equals(right) : right is null;

    public static bool operator !=(DisplayString? left, DisplayString? right) => !(left == right);

    public static bool operator ==(DisplayString? left, string? right) => left is not null ? left.Equals(right) : right is null;

    public static bool operator !=(DisplayString? left, string? right) => !(left == right);

    public static bool operator ==(string? left, DisplayString? right) => right is not null ? right.Equals(left) : left is null;

    public static bool operator !=(string? left, DisplayString? right) => !(left == right);

    public override string ToString() => _value;

#pragma warning disable CA1307 // Specify StringComparison for clarity -- not available in all targets
    public override int GetHashCode() => _value.GetHashCode();
#pragma warning restore CA1307 // Specify StringComparison for clarity

    public bool Equals(DisplayString? other) => other is not null && _value == other._value;

    public bool Equals(string? other) => _value.Equals(other, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj switch
    {
        DisplayString other => Equals(other),
        string other => Equals(other),
        _ => false,
    };
}
