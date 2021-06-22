// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HttpLogging
{
    public class W3CLoggerTests
    {

        DateTime _timestampOne = new DateTime(2021, 01, 02, 03, 04, 05);

        [Fact]
        public async Task WritesDateTime()
        {
            var path = Path.GetTempFileName() + "_";
            var now = DateTime.Now;
            var options = new W3CLoggerOptions()
            {
                LoggingFields = W3CLoggingFields.Date | W3CLoggingFields.Time | W3CLoggingFields.TimeTaken,
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
                    await logger.WaitForWrites(4).DefaultTimeout();

                    var lines = logger.Processor.Lines;
                    Assert.Equal("#Version: 1.0", lines[0]);

                    Assert.StartsWith("#Start-Date: ", lines[1]);
                    var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
                    // Assert that the log was written in the last 10 seconds
                    Assert.True(now.Subtract(startDate).TotalSeconds < 10);

                    Assert.Equal("#Fields: date time time-taken", lines[2]);

                    Assert.StartsWith("2021-01-02 03:04:05 ", lines[3]);
                    // Assert that the log's time-taken is within 10 seconds of DateTime.Now minus our arbitary start time (01/02/21 at 3:04:05)
                    Assert.True(now.Subtract(_timestampOne).TotalSeconds - Convert.ToDouble(lines[3].Substring(20), CultureInfo.InvariantCulture) < 10);
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
            var now = DateTime.Now;
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
                    await logger.WaitForWrites(4).DefaultTimeout();

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
}
