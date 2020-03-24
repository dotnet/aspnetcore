// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace BasicTestApp
{
    // The goal for this class is to make it possible for E2E tests to observe that a custom
    // logger factory can be plugged in and gets used when logging unhandled exceptions.
    // However, it's valuable to pass through all calls to the default implementation too
    // so that if any defect in the underlying implementation would break tests, we still see it.

    public class PrependMessageLoggerFactory : ILoggerFactory
    {
        private readonly string _message;
        private readonly ILoggerFactory _underlyingFactory;

        public PrependMessageLoggerFactory(string message, ILoggerFactory underlyingFactory)
        {
            _message = message;
            _underlyingFactory = underlyingFactory;
        }

        public void AddProvider(ILoggerProvider provider)
            => _underlyingFactory.AddProvider(provider);

        public ILogger CreateLogger(string categoryName)
            => new PrependMessageLogger(_message, _underlyingFactory.CreateLogger(categoryName));

        public void Dispose()
            => _underlyingFactory.Dispose();

        private class PrependMessageLogger : ILogger
        {
            private readonly string _message;
            private readonly ILogger _underlyingLogger;

            public PrependMessageLogger(string message, ILogger underlyingLogger)
            {
                _message = message;
                _underlyingLogger = underlyingLogger;
            }

            public IDisposable BeginScope<TState>(TState state)
                => _underlyingLogger.BeginScope(state);

            public bool IsEnabled(LogLevel logLevel)
                => _underlyingLogger.IsEnabled(logLevel);

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                => _underlyingLogger.Log(logLevel, eventId, state, exception,
                    (state, exception) => $"[{_message}] {formatter(state, exception)}");
        }
    }
}
