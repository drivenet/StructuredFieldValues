using System;
using System.Globalization;

namespace StructuredFieldValues
{
#pragma warning disable CA1815 // Override equals and operator equals on value types -- not needed here
    public readonly partial struct ParseResult<TResult>
#pragma warning restore CA1815 // Override equals and operator equals on value types
        where TResult : notnull
    {
        private readonly TResult? _result;
        private readonly string? _message;
        private readonly int _offset;

        public ParseResult(TResult result)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
            _message = null;
            _offset = 0;
        }

        public ParseResult(int offset, string message)
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
                throw new ArgumentException("Error cannot be empty or empty.", nameof(message));
            }

            _result = default;
            _message = message;
            _offset = offset;
        }

        public ParseResult(int offset, string format, params object[] args)
            : this(offset, string.Format(CultureInfo.InvariantCulture, format, args))
        {
        }

        private TResult Result => _result is { } result
            ? result
            : throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "No result {0}.", typeof(TResult)));

        public void Apply(Action<TResult> resultHandler, Action<int, string> errorHandler)
        {
            if (_message is { } message)
            {
                errorHandler(_offset, message);
            }
            else
            {
                resultHandler(Result);
            }
        }

        public TResult Unwrap()
        {
            if (_message is { } message)
            {
                throw new FormatException(FormattableString.Invariant($"Failed to parse: {message} at offset {_offset}."));
            }

            return Result;
        }

        public ParseResult<TOtherResult> Map<TOtherResult>(Func<TResult, TOtherResult> map)
            where TOtherResult : notnull
        {
            if (_message is { } message)
            {
                return new ParseResult<TOtherResult>(_offset, message);
            }

            return new ParseResult<TOtherResult>(map(Result));
        }
    }
}
