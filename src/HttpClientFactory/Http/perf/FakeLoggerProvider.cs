// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Performance
{
    internal class FakeLoggerProvider : ILoggerProvider
    {
        public bool IsEnabled { get; set; }

        public ILogger CreateLogger(string categoryName)
        {
            return new Logger(this);
        }

        public void Dispose()
        {
        }

        private class Logger : ILogger
        {
            private FakeLoggerProvider _provider;

            public Logger(FakeLoggerProvider provider)
            {
                _provider = provider;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return _provider.IsEnabled;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
            }
        }
    }
}
