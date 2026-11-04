// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Logging.Testing.Tests;

public class XunitLoggerProviderTest
{
    [Fact]
    public void LoggerProviderWritesToTestOutputHelper()
    {
        var testTestOutputHelper = new TestTestOutputHelper();

        var loggerFactory = CreateTestLogger(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddXunit(testTestOutputHelper));

        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.LogInformation("This is some great information");
        logger.LogTrace("This is some unimportant information");

        var expectedOutput =
            "| [TIMESTAMP] TestCategory Information: This is some great information" + Environment.NewLine +
            "| [TIMESTAMP] TestCategory Trace: This is some unimportant information" + Environment.NewLine;

        Assert.Equal(expectedOutput, MakeConsistent(testTestOutputHelper.Output));
    }

    [Fact]
    public void LoggerProviderDoesNotWriteLogMessagesBelowMinimumLevel()
    {
        var testTestOutputHelper = new TestTestOutputHelper();
        var loggerFactory = CreateTestLogger(builder => builder
            .AddXunit(testTestOutputHelper, LogLevel.Warning));

        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.LogInformation("This is some great information");
        logger.LogError("This is a bad error");

        Assert.Equal("| [TIMESTAMP] TestCategory Error: This is a bad error" + Environment.NewLine, MakeConsistent(testTestOutputHelper.Output));
    }

    [Fact]
    public void LoggerProviderPrependsPrefixToEachLine()
    {
        var testTestOutputHelper = new TestTestOutputHelper();
        var loggerFactory = CreateTestLogger(builder => builder
            .AddXunit(testTestOutputHelper));

        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.LogInformation("This is a" + Environment.NewLine + "multi-line" + Environment.NewLine + "message");

        // The lines after the first one are indented more because the indentation was calculated based on the timestamp's actual length.
        var expectedOutput =
            "| [TIMESTAMP] TestCategory Information: This is a" + Environment.NewLine +
            "|                                                 multi-line" + Environment.NewLine +
            "|                                                 message" + Environment.NewLine;

        Assert.Equal(expectedOutput, MakeConsistent(testTestOutputHelper.Output));
    }

    [Fact]
    public void LoggerProviderDoesNotThrowIfOutputHelperThrows()
    {
        var testTestOutputHelper = new TestTestOutputHelper();
        var loggerFactory = CreateTestLogger(builder => builder

            .AddXunit(testTestOutputHelper));

        testTestOutputHelper.Throw = true;

        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.LogInformation("This is a" + Environment.NewLine + "multi-line" + Environment.NewLine + "message");

        Assert.Equal(0, testTestOutputHelper.Output.Length);
    }

    private static readonly Regex TimestampRegex = new Regex(@"\d+-\d+-\d+T\d+:\d+:\d+");

    private string MakeConsistent(string input) => TimestampRegex.Replace(input, "TIMESTAMP");

    private static ILoggerFactory CreateTestLogger(Action<ILoggingBuilder> configure)
    {
        return new ServiceCollection()
            .AddLogging(configure)
            .BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>();
    }
}
