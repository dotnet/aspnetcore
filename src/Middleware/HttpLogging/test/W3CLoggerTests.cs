// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.HttpLogging;

public class W3CLoggerTests
{
    readonly DateTime _timestampOne = new DateTime(2021, 01, 02, 03, 04, 05);

    [Fact]
    public async Task WritesDateTime()
    {
        var path = Path.GetTempFileName() + "_";
        var now = DateTime.Now;
        var options = new W3CLoggerOptions()
        {
            LoggingFields = W3CLoggingFields.Date | W3CLoggingFields.Time,
            LogDirectory = path
        };
        try
        {
            await using (var logger = new TestW3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                var elements = new string[W3CLoggingMiddleware._fieldsLength];
                AddToList(elements, W3CLoggingMiddleware._dateIndex, _timestampOne.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                AddToList(elements, W3CLoggingMiddleware._timeIndex, _timestampOne.ToString("HH:mm:ss", CultureInfo.InvariantCulture));

                logger.Log(elements);
                await logger.Processor.WaitForWrites(4).DefaultTimeout();

                var lines = logger.Processor.Lines;
                Assert.Equal("#Version: 1.0", lines[0]);

                Assert.StartsWith("#Start-Date: ", lines[1]);
                var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
                // Assert that the log was written in the last 10 seconds
                Assert.True(now.Subtract(startDate).TotalSeconds < 10);

                Assert.Equal("#Fields: date time", lines[2]);

                Assert.StartsWith("2021-01-02 03:04:05", lines[3]);
            }
        }
        finally
        {
            Helpers.DisposeDirectory(path);
        }
    }

    [Fact]
    public async Task HandlesNullValuesAsync()
    {
        var path = Path.GetTempFileName() + "_";
        var now = DateTime.UtcNow;
        var options = new W3CLoggerOptions()
        {
            LoggingFields = W3CLoggingFields.UriQuery | W3CLoggingFields.Host | W3CLoggingFields.ProtocolStatus,
            LogDirectory = path
        };
        try
        {
            await using (var logger = new TestW3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                var elements = new string[W3CLoggingMiddleware._fieldsLength];
                AddToList(elements, W3CLoggingMiddleware._uriQueryIndex, null);
                AddToList(elements, W3CLoggingMiddleware._hostIndex, null);
                AddToList(elements, W3CLoggingMiddleware._protocolStatusIndex, null);

                logger.Log(elements);
                await logger.Processor.WaitForWrites(4).DefaultTimeout();

                var lines = logger.Processor.Lines;
                Assert.Equal("#Version: 1.0", lines[0]);

                Assert.StartsWith("#Start-Date: ", lines[1]);
                var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
                // Assert that the log was written in the last 10 seconds
                Assert.True(now.Subtract(startDate).TotalSeconds < 10);

                Assert.Equal("#Fields: cs-uri-query sc-status cs-host", lines[2]);
                Assert.Equal("- - -", lines[3]);
            }
        }
        finally
        {
            Helpers.DisposeDirectory(path);
        }
    }

    private void AddToList(string[] elements, int index, string value)
    {
        value ??= string.Empty;
        elements[index] = value;
    }
}
