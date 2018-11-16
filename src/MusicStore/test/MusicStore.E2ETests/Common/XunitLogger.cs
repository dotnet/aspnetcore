// Copyright (c) .NET Foundation. All rights reserved.
// See License.txt in the project root for license information

using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using System.Linq;

namespace E2ETests.Common
{
    public class XunitLogger : ILogger, IDisposable
    {
        private readonly LogLevel _minLogLevel;
        private readonly ITestOutputHelper _output;
        private bool _disposed;

        public XunitLogger(ITestOutputHelper output, LogLevel minLogLevel)
        {
            _minLogLevel = minLogLevel;
            _output = output;
        }

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var firstLinePrefix = "| " + logLevel + ": ";
            var lines = formatter(state, exception).Split('\n');
            _output.WriteLine(firstLinePrefix + lines.First());

            var additionalLinePrefix = "|" + new string(' ', firstLinePrefix.Length - 1);
            foreach (var line in lines.Skip(1))
            {
                _output.WriteLine(additionalLinePrefix + line.Trim('\r'));
            }
        }

        public bool IsEnabled(LogLevel logLevel)
            => logLevel >= _minLogLevel && !_disposed;

        public IDisposable BeginScope<TState>(TState state)
            => new NullScope();

        private class NullScope : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
