// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;

namespace BasicTestApp
{
    // The goal for this class is to make it possible for E2E tests to observe that a custom
    // logger factory can be plugged in and gets used when logging unhandled exceptions.
    // However, it's valuable to pass through all calls to the default implementation too
    // so that if any defect in the underlying implementation would break tests, we still see it.

    public class PrependMessageLoggerProvider: ILoggerProvider
    {
        ILogger _logger;
        IConfiguration _configuration;
        ILogger _defaultLogger;
        private bool _disposed = false;

        public PrependMessageLoggerProvider(IConfiguration configuration, IJSRuntime runtime)
        {
            _configuration = configuration;
            _defaultLogger = new WebAssemblyConsoleLogger<object>(runtime);
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_logger == null)
            {
                var message = _configuration["Logging:PrependMessage:Message"];
                _logger = new PrependMessageLogger(message, _defaultLogger);
            }
            return _logger;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger = null;
            }
            _disposed = true;
        }

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
