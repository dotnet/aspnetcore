// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Testing;

public class TestLoggerProvider : ILoggerProvider
{
    private readonly ITestSink _sink;

    public TestLoggerProvider(ITestSink sink)
    {
        _sink = sink;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(categoryName, _sink, enabled: true);
    }

    public void Dispose()
    {
    }
}
