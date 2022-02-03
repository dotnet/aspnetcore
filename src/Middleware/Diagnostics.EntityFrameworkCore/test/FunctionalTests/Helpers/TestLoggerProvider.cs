// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.FunctionalTests.Helpers;

public class TestLoggerProvider : ILoggerProvider
{
    private readonly TestLogger _logger = new TestLogger();

    public TestLogger Logger
    {
        get { return _logger; }
    }

    public ILogger CreateLogger(string name)
    {
        return _logger;
    }

    public void Dispose()
    {
    }

    public class TestLogger : ILogger
    {
        private readonly List<string> _messages = new List<string>();

        private readonly object _sync = new object();

        public IEnumerable<string> Messages
        {
            get
            {
                lock (_sync)
                {
                    return new List<string>(_messages);
                }
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (_sync)
            {
                _messages.Add(formatter(state, exception));
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public class NullScope : IDisposable
        {
            public static NullScope Instance = new NullScope();

            public void Dispose()
            { }
        }
    }
}
