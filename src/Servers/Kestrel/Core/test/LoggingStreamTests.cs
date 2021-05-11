// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class LoggingStreamTests
    {
        [Theory]
        [InlineData( 1, "00                                                 .")]
        [InlineData( 2, "00 00                                              ..")]
        [InlineData( 3, "00 00 00                                           ...")]
        [InlineData( 4, "00 00 00 00                                        ....")]
        [InlineData( 5, "00 00 00 00 00                                     .....")]
        [InlineData( 6, "00 00 00 00 00 00                                  ......")]
        [InlineData( 7, "00 00 00 00 00 00 00                               .......")]
        [InlineData( 8, "00 00 00 00 00 00 00 00                            ........ ")]
        [InlineData( 9, "00 00 00 00 00 00 00 00  00                        ........ .")]
        [InlineData(10, "00 00 00 00 00 00 00 00  00 00                     ........ ..")]
        [InlineData(11, "00 00 00 00 00 00 00 00  00 00 00                  ........ ...")]
        [InlineData(12, "00 00 00 00 00 00 00 00  00 00 00 00               ........ ....")]
        [InlineData(13, "00 00 00 00 00 00 00 00  00 00 00 00 00            ........ .....")]
        [InlineData(14, "00 00 00 00 00 00 00 00  00 00 00 00 00 00         ........ ......")]
        [InlineData(15, "00 00 00 00 00 00 00 00  00 00 00 00 00 00 00      ........ .......")]
        [InlineData(16, "00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00   ........ ........")]
        public void CorrectPaddingIsUsedAfterHexValues(int bufferLength, string expectedOutput)
        {
            var mockLogger = new MockLogger();
            var loggingStream = new LoggingStream(Stream.Null, mockLogger);

            loggingStream.Write(new byte[bufferLength]);

            Assert.Equal($"Write[{bufferLength}]{Environment.NewLine}{expectedOutput}", mockLogger.Logs);
        }

        [Fact]
        public void ExtraNewLineIsNotLoggedGivenEmptyBuffer()
        {
            var mockLogger = new MockLogger();
            var loggingStream = new LoggingStream(Stream.Null, mockLogger);

            loggingStream.Write(default);

            Assert.Equal($"Write[0]", mockLogger.Logs);
        }

        private class MockLogger : ILogger
        {
            private StringBuilder _logs = new();

            public string Logs => _logs.ToString();

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _logs.Append(formatter(state, exception));
            }
        }
    }
}
