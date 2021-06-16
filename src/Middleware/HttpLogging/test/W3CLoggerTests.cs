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
                using (var logger = new TestW3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(options), 4))
                {
                    var state = new List<KeyValuePair<string, string>>();
                    state.Add(new KeyValuePair<string, string>(nameof(DateTime), _timestampOne.ToString(CultureInfo.InvariantCulture)));

                    logger.Log(state);
                    await logger.WaitForWrites().DefaultTimeout();

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
        public async Task HandlesNullValues()
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
                using (var logger = new TestW3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(options), 4))
                {
                    var state = new List<KeyValuePair<string, string>>();
                    state.Add(new KeyValuePair<string, string>(nameof(HttpRequest.QueryString), null));
                    state.Add(new KeyValuePair<string, string>(nameof(HeaderNames.Host), null));
                    state.Add(new KeyValuePair<string, string>(nameof(HttpResponse.StatusCode), null));

                    logger.Log(state);
                    await logger.WaitForWrites().DefaultTimeout();

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
    }
}
