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

    [Fact]
    public async Task WritesOnlySelectedFieldsInOrder()
    {
        var path = Path.GetTempFileName() + "_";
        var options = new W3CLoggerOptions()
        {
            // Selected fields are not contiguous - make sure unselected fields in between are skipped
            // entirely rather than showing up as extra dashes.
            LoggingFields = W3CLoggingFields.Method | W3CLoggingFields.UriStem | W3CLoggingFields.ProtocolStatus,
            LogDirectory = path
        };
        try
        {
            await using (var logger = Helpers.CreateTestW3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(options)))
            {
                var elements = new string[W3CLoggingMiddleware._fieldsLength];
                var additionalHeaders = new string[0];
                AddToList(elements, W3CLoggingMiddleware._methodIndex, "GET");
                AddToList(elements, W3CLoggingMiddleware._uriStemIndex, "/foo");
                AddToList(elements, W3CLoggingMiddleware._protocolStatusIndex, "200");
                // Not selected - must not appear in the output even though it has a value.
                AddToList(elements, W3CLoggingMiddleware._uriQueryIndex, "?bar=baz");

                logger.Log(elements, additionalHeaders);
                await logger.Processor.WaitForWrites(4).DefaultTimeout();

                var lines = logger.Processor.Lines;
                Assert.Equal("#Fields: cs-method cs-uri-stem sc-status", lines[2]);
                Assert.Equal("GET /foo 200", lines[3]);
            }
        }
        finally
        {
            Helpers.DisposeDirectory(path);
        }
    }

    [Fact]
    public async Task WritesAdditionalHeadersAfterFieldsPreservingOrderAndDashes()
    {
        var path = Path.GetTempFileName() + "_";
        var options = new W3CLoggerOptions()
        {
            LoggingFields = W3CLoggingFields.Method,
            LogDirectory = path
        };
        // Additional headers must actually be configured for the #Fields: directive and the data
        // line to agree, matching what the middleware would produce - a data line with additional
        // header values but no corresponding entries in the directive cannot happen in production.
        options.AdditionalRequestHeaders.Add("X-First");
        options.AdditionalRequestHeaders.Add("X-Second");
        options.AdditionalRequestHeaders.Add("X-Third");
        try
        {
            await using (var logger = Helpers.CreateTestW3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(options)))
            {
                var elements = new string[W3CLoggingMiddleware._fieldsLength];
                // Additional headers are supplied in a specific order by the caller (the middleware
                // orders them to match options.AdditionalRequestHeaders); W3CLogger/W3CLoggerProcessor
                // must preserve that order verbatim.
                var additionalHeaders = new[] { "first", "", "third" };
                AddToList(elements, W3CLoggingMiddleware._methodIndex, "POST");

                logger.Log(elements, additionalHeaders);
                await logger.Processor.WaitForWrites(4).DefaultTimeout();

                var lines = logger.Processor.Lines;
                Assert.Equal("#Fields: cs-method cs(X-First) cs(X-Second) cs(X-Third)", lines[2]);
                Assert.Equal("POST first - third", lines[3]);
            }
        }
        finally
        {
            Helpers.DisposeDirectory(path);
        }
    }

    [Fact]
    public async Task MatchesExpectedFormatForAllFieldsPlusAdditionalHeaders()
    {
        var path = Path.GetTempFileName() + "_";
        var options = new W3CLoggerOptions()
        {
            LoggingFields = W3CLoggingFields.All,
            LogDirectory = path
        };
        // Register the additional header so the #Fields: directive and the data line agree, matching
        // what the middleware would actually produce.
        options.AdditionalRequestHeaders.Add("X-Extra");
        try
        {
            await using (var logger = Helpers.CreateTestW3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(options)))
            {
                var elements = new string[W3CLoggingMiddleware._fieldsLength];
                AddToList(elements, W3CLoggingMiddleware._dateIndex, "2021-01-02");
                AddToList(elements, W3CLoggingMiddleware._timeIndex, "03:04:05");
                AddToList(elements, W3CLoggingMiddleware._clientIpIndex, "127.0.0.1");
                AddToList(elements, W3CLoggingMiddleware._userNameIndex, null);
                AddToList(elements, W3CLoggingMiddleware._serverNameIndex, "my-server");
                AddToList(elements, W3CLoggingMiddleware._serverIpIndex, "127.0.0.2");
                AddToList(elements, W3CLoggingMiddleware._serverPortIndex, "443");
                AddToList(elements, W3CLoggingMiddleware._methodIndex, "GET");
                AddToList(elements, W3CLoggingMiddleware._uriStemIndex, "/foo");
                AddToList(elements, W3CLoggingMiddleware._uriQueryIndex, "");
                AddToList(elements, W3CLoggingMiddleware._protocolStatusIndex, "200");
                AddToList(elements, W3CLoggingMiddleware._timeTakenIndex, "1.234");
                AddToList(elements, W3CLoggingMiddleware._protocolVersionIndex, "HTTP/1.1");
                AddToList(elements, W3CLoggingMiddleware._hostIndex, "localhost");
                AddToList(elements, W3CLoggingMiddleware._userAgentIndex, "test-agent");
                AddToList(elements, W3CLoggingMiddleware._refererIndex, null);
                var additionalHeaders = new[] { "extra-value" };

                logger.Log(elements, additionalHeaders);
                await logger.Processor.WaitForWrites(4).DefaultTimeout();

                var lines = logger.Processor.Lines;
                Assert.Equal(
                    "#Fields: date time c-ip cs-username s-computername s-ip s-port cs-method cs-uri-stem cs-uri-query sc-status time-taken cs-version cs-host cs(User-Agent) cs(Cookie) cs(Referer) cs(X-Extra)",
                    lines[2]);
                Assert.Equal(
                    "2021-01-02 03:04:05 127.0.0.1 - my-server 127.0.0.2 443 GET /foo - 200 1.234 HTTP/1.1 localhost test-agent - - extra-value",
                    lines[3]);
            }
        }
        finally
        {
            Helpers.DisposeDirectory(path);
        }
    }

    [Fact]
    public async Task PreservesLoggingFieldsCapturedAtEnqueueTime()
    {
        var path = Path.GetTempFileName() + "_";
        var options = new W3CLoggerOptions()
        {
            LoggingFields = W3CLoggingFields.Date,
            LogDirectory = path
        };
        var monitor = new OptionsWrapperMonitor<W3CLoggerOptions>(options);
        try
        {
            await using (var logger = Helpers.CreateTestW3CLogger(monitor))
            {
                var elements = new string[W3CLoggingMiddleware._fieldsLength];
                var additionalHeaders = new string[0];
                AddToList(elements, W3CLoggingMiddleware._dateIndex, _timestampOne.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                AddToList(elements, W3CLoggingMiddleware._timeIndex, _timestampOne.ToString("HH:mm:ss", CultureInfo.InvariantCulture));

                // Arm a one-shot gate that pauses the background processor immediately after it
                // dequeues this entry, before any of its fields are read.
                var entryReached = logger.Processor.ArmEntryGate();

                // Log while only Date is selected...
                logger.Log(elements, additionalHeaders);

                // ...deterministically wait until the processor has reached (but not yet serialized)
                // the entry...
                await entryReached.DefaultTimeout();

                // ...only now change which fields are selected. If production incorrectly read the
                // consumer-time LoggingFields instead of the value captured at Log()/enqueue time,
                // this change would be reflected in the output below. The gate is released in
                // `finally` so a failure here can't leave the background processor - and this
                // await-using block's DisposeAsync - blocked on the gate forever.
                try
                {
                    options.LoggingFields = W3CLoggingFields.Time;
                    monitor.InvokeChanged();
                }
                finally
                {
                    logger.Processor.ReleaseGatedWrite();
                }

                await logger.Processor.WaitForWrites(4).DefaultTimeout();

                var lines = logger.Processor.Lines;
                Assert.Equal(_timestampOne.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), lines[3]);

                // A subsequent entry, logged after the change has taken effect, must reflect the new
                // LoggingFields (Time) rather than staying pinned to whatever the first entry saw.
                logger.Log(elements, additionalHeaders);
                await logger.Processor.WaitForWrites(5).DefaultTimeout();

                Assert.Equal(_timestampOne.ToString("HH:mm:ss", CultureInfo.InvariantCulture), lines[4]);
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
