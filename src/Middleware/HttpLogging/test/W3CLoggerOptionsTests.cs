// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpLogging;

public class W3CLoggerOptionsTests
{
    [Fact]
    public void DoesNotInitializeWithOptionalFields()
    {
        var options = new W3CLoggerOptions();
        // Optional fields shouldn't be logged by default
        Assert.False(options.LoggingFields.HasFlag(W3CLoggingFields.UserName));
        Assert.False(options.LoggingFields.HasFlag(W3CLoggingFields.Cookie));
    }

    [Fact]
    public void ThrowsOnNegativeFileSizeLimit()
    {
        var options = new W3CLoggerOptions();
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.FileSizeLimit = -1);
        Assert.Contains("FileSizeLimit must be positive", ex.Message);
    }

    [Fact]
    public void ThrowsOnEmptyFileName()
    {
        var options = new W3CLoggerOptions();
        Assert.Throws<ArgumentException>(() => options.FileName = "");
    }

    [Fact]
    public void ThrowsOnEmptyLogDirectory()
    {
        var options = new W3CLoggerOptions();
        Assert.Throws<ArgumentException>(() => options.LogDirectory = "");
    }

    [Fact]
    public void ThrowsOnNegativeFlushInterval()
    {
        var options = new W3CLoggerOptions();
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.FlushInterval = TimeSpan.FromSeconds(-1));
        Assert.Contains("FlushInterval must be positive", ex.Message);
    }
}
