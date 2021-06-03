using System;

using Xunit;

namespace StructuredFieldValues.Tests
{
    public class ParseResultTests
    {
        [Fact]
        public void DefaultReferenceTypeValueThrows()
        {
            Assert.ThrowsAny<Exception>(() => default(ParseResult<string>).Unwrap());
        }

        [Fact]
        public void DefaultValueTypeValueDoesntThrow()
        {
            Assert.Equal(0, default(ParseResult<int>).Unwrap());
        }
    }
}
