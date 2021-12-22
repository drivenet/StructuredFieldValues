using System;
using System.Globalization;

namespace StructuredFieldValues;

/// <summary>
///     Represents an error that occured while parsing structured field values.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types -- not needed here
public readonly struct ParseError : IFormattable
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    private readonly string _message;

    public ParseError(int offset, string message)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Negative error offset.");
        }

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (message.Length == 0)
        {
            throw new ArgumentException("Error cannot be empty.", nameof(message));
        }

        _message = message;
        Offset = offset;
    }

    public ParseError(int offset, string format, params object[] args)
        : this(offset, string.Format(CultureInfo.InvariantCulture, format, args))
    {
    }

    public string Message => _message ?? "unknown error";

    public int Offset { get; }

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider) => $"Failed to parse: {Message} at offset {Offset}.";

    public FormatException ToException() => new FormatException(ToString());
}
