// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;

namespace BasicTestApp
{
    [ProviderAlias("PrependMessage")]
    internal class PrependMessageLoggerProvider : ILoggerProvider
    {
        ILogger _logger;
        string _message;
        ILogger _defaultLogger;
        private bool _disposed = false;

        public PrependMessageLoggerProvider(string message, IJSRuntime runtime)
        {
            _message = message;
            _defaultLogger = new WebAssemblyConsoleLogger<object>(runtime);
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_logger == null)
            {
                _logger = new PrependMessageLogger(_message, _defaultLogger);
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
