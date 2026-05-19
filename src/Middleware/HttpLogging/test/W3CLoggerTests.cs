// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.HttpLogging;

public class W3CLoggerTests
{
    readonly DateTime _timestampOne = new DateTime(2021, 01, 02, 03, 04, 05);

    [Fact]
    public async Task WritesDateTime()
    {
        var path = Path.GetTempFileName() + "_";
        var now = DateTime.UtcNow;
        var options = new W3CLoggerOptions()
        {
            LoggingFields = W3CLoggingFields.Date | W3CLoggingFields.Time,
            LogDirectory = path
        };
        try
        {
            await using (var logger = Helpers.CreateTestW3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(options)))
            {
                var elements = new string[W3CLoggingMiddleware._fieldsLength];
                var additionalHeaders = new string[0];
                AddToList(elements, W3CLoggingMiddleware._dateIndex, _timestampOne.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                AddToList(elements, W3CLoggingMiddleware._timeIndex, _timestampOne.ToString("HH:mm:ss", CultureInfo.InvariantCulture));

                logger.Log(elements, additionalHeaders);
                await logger.Processor.WaitForWrites(4).DefaultTimeout();

                var lines = logger.Processor.Lines;
                Assert.Equal("#Version: 1.0", lines[0]);

                Assert.StartsWith("#Start-Date: ", lines[1]);
                var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
                // Assert that the log was written in the last 10 seconds
                // W3CLogger writes start-time to second precision, so delta could be as low as -0.999...
                var delta = startDate.Subtract(now).TotalSeconds;
                Assert.InRange(delta, -1, 10);

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
            await using (var logger = Helpers.CreateTestW3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(options)))
            {
                var elements = new string[W3CLoggingMiddleware._fieldsLength];
                var additionalHeaders = new string[0];
                AddToList(elements, W3CLoggingMiddleware._uriQueryIndex, null);
                AddToList(elements, W3CLoggingMiddleware._hostIndex, null);
                AddToList(elements, W3CLoggingMiddleware._protocolStatusIndex, null);

                logger.Log(elements, additionalHeaders);
                await logger.Processor.WaitForWrites(4).DefaultTimeout();

                var lines = logger.Processor.Lines;
                Assert.Equal("#Version: 1.0", lines[0]);

                Assert.StartsWith("#Start-Date: ", lines[1]);
                var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
                // Assert that the log was written in the last 10 seconds
                // W3CLogger writes start-time to second precision, so delta could be as low as -0.999...
                var delta = startDate.Subtract(now).TotalSeconds;
                Assert.InRange(delta, -1, 10);

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
