using System;

namespace Xunit.Sdk;

internal sealed class TestFailedException : XunitException
{
    public TestFailedException(string userMessage, Exception innerException)
        : base(userMessage, innerException)
    {
    }
}
