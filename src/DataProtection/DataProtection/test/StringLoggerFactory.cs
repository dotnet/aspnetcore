// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection;

internal sealed class StringLoggerFactory : ILoggerFactory
{
    private readonly StringBuilder _log = new StringBuilder();

    public StringLoggerFactory(LogLevel logLevel)
    {
        MinimumLevel = logLevel;
    }

    public LogLevel MinimumLevel { get; set; }

    public void AddProvider(ILoggerProvider provider)
    {
        // no-op
    }

    public ILogger CreateLogger(string name)
    {
        return new StringLogger(name, this);
    }

    public void Dispose()
    {
    }

    public override string ToString()
    {
        return _log.ToString();
    }

    private sealed class StringLogger : ILogger
    {
        private readonly StringLoggerFactory _factory;
        private readonly string _name;

        public StringLogger(string name, StringLoggerFactory factory)
        {
            _name = name;
            _factory = factory;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new DummyDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (logLevel >= _factory.MinimumLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = string.Format(CultureInfo.InvariantCulture,
                "Provider: {0}" + Environment.NewLine +
                "Log level: {1}" + Environment.NewLine +
                "Event id: {2}" + Environment.NewLine +
                "Exception: {3}" + Environment.NewLine +
                "Message: {4}", _name, logLevel, eventId, exception?.ToString(), formatter(state, exception));
            _factory._log.AppendLine(message);
        }

        private sealed class DummyDisposable : IDisposable
        {
            public void Dispose()
            {
                // no-op
            }
        }
    }
}
