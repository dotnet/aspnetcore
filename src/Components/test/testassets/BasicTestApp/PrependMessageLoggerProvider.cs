// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;

namespace BasicTestApp;

[ProviderAlias("PrependMessage")]
internal class PrependMessageLoggerProvider : ILoggerProvider
{
    ILogger _logger;
    readonly string _message;
    readonly ILogger _defaultLogger;
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
