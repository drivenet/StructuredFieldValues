using System;

namespace StructuredFieldValues;

public sealed class Token : IEquatable<Token>, IEquatable<string>
{
    public static readonly Token Empty = new("");

    private readonly string _value;

    public Token(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static implicit operator string(Token token) => token._value;

    public static bool operator ==(Token? left, Token? right) => left is not null ? left.Equals(right) : right is null;

    public static bool operator !=(Token? left, Token? right) => !(left == right);

    public static bool operator ==(Token? left, string? right) => left is not null ? left.Equals(right) : right is null;

    public static bool operator !=(Token? left, string? right) => !(left == right);

    public static bool operator ==(string? left, Token? right) => right is not null ? right.Equals(left) : left is null;

    public static bool operator !=(string? left, Token? right) => !(left == right);

    public override string ToString() => _value;

#pragma warning disable CA1307 // Specify StringComparison for clarity -- not available in all targets
    public override int GetHashCode() => _value.GetHashCode();
#pragma warning restore CA1307 // Specify StringComparison for clarity

    public bool Equals(Token? other) => other is not null && _value == other._value;

    public bool Equals(string? other) => _value.Equals(other, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj switch
    {
        Token other => Equals(other),
        string other => Equals(other),
        _ => false,
    };
}
