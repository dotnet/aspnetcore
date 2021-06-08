using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HttpLogging
{
    public class W3CLoggerTests : IDisposable
    {

        DateTime _timestampOne = new DateTime(2021, 01, 02, 03, 04, 05);

        public W3CLoggerTests()
        {
            TempPath = Path.GetTempFileName() + "_";
        }

        public string TempPath { get; }
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(TempPath))
                {
                    Directory.Delete(TempPath, true);
                }
            }
            catch
            {
                // ignored
            }
        }

        [Fact]
        public void WritesToTextFile()
        {
            string fileName;
            using (var logger = new W3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(new W3CLoggerOptions()
            {
                LoggingFields = W3CLoggingFields.Date | W3CLoggingFields.Time | W3CLoggingFields.TimeTaken,
                LogDirectory = TempPath
            })))
            {
                var state = new List<KeyValuePair<string, object>>();
                state.Add(new KeyValuePair<string, object>(nameof(DateTime), _timestampOne));

                logger.Log(state);
                fileName = logger.LogFileFullName;
            }

            var lines = File.ReadAllLines(fileName);
            Assert.Equal("#Version: 1.0", lines[0]);
            Assert.StartsWith("#Start-Date: ", lines[1]);
            var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
            // Assert that the log was written in the last 10 seconds
            var now = DateTime.Now;
            Assert.True(now.Subtract(startDate).TotalSeconds < 10);

            Assert.Equal("#Fields: date time time-taken", lines[2]);
            Assert.StartsWith("2021-01-02 03:04:05 ", lines[3]);
            // Assert that the log's time-taken is within 10 seconds of DateTime.Now minus our arbitary start time (01/02/21 at 3:04:05)
            Assert.True(now.Subtract(_timestampOne).TotalSeconds - Convert.ToDouble(lines[3].Substring(20), CultureInfo.InvariantCulture) < 10);
        }

        [Fact]
        public void HandlesNullValues()
        {
            string fileName;
            using (var logger = new W3CLogger(new OptionsWrapperMonitor<W3CLoggerOptions>(new W3CLoggerOptions()
            {
                LoggingFields = W3CLoggingFields.UriQuery | W3CLoggingFields.Host | W3CLoggingFields.ProtocolStatus,
                LogDirectory = TempPath
            })))
            {
                var state = new List<KeyValuePair<string, object>>();
                state.Add(new KeyValuePair<string, object>(nameof(HttpRequest.QueryString), null));
                state.Add(new KeyValuePair<string, object>(nameof(HeaderNames.Host), null));
                state.Add(new KeyValuePair<string, object>(nameof(HttpResponse.StatusCode), null));

                logger.Log(state);
                fileName = logger.LogFileFullName;
            }

            var lines = File.ReadAllLines(fileName);
            Assert.Equal("#Version: 1.0", lines[0]);
            Assert.StartsWith("#Start-Date: ", lines[1]);
            var startDate = DateTime.Parse(lines[1].Substring(13), CultureInfo.InvariantCulture);
            // Assert that the log was written in the last 10 seconds
            var now = DateTime.Now;
            Assert.True(now.Subtract(startDate).TotalSeconds < 10);

            Assert.Equal("#Fields: cs-uri-query sc-status cs-host", lines[2]);
            Assert.Equal("- - -", lines[3]);
        }
    }
}
