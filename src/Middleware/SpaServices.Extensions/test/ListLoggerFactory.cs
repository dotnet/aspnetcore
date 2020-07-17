using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SpaServices.Extensions.Tests
{
    public class ListLoggerFactory : ILoggerFactory
    {
        private readonly Func<string, bool> _shouldLogCategory;
        private bool _disposed;

        public ListLoggerFactory()
            : this(_ => true)
        {
        }

        public ListLoggerFactory(Func<string, bool> shouldLogCategory)
        {
            _shouldLogCategory = shouldLogCategory;
            Logger = new ListLogger();
        }

        public List<(LogLevel Level, EventId Id, string Message, object State, Exception Exception)> Log => Logger.LoggedEvents;
        protected ListLogger Logger { get; set; }

        public virtual void Clear() => Logger.Clear();

        public void SetTestOutputHelper(ITestOutputHelper testOutputHelper)
        {
            Logger.TestOutputHelper = testOutputHelper;
        }

        public virtual ILogger CreateLogger(string name)
        {
            CheckDisposed();

            return !_shouldLogCategory(name)
                ? (ILogger)NullLogger.Instance
                : Logger;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ListLoggerFactory));
            }
        }

        public void AddProvider(ILoggerProvider provider)
        {
            CheckDisposed();
        }

        public void Dispose()
        {
            _disposed = true;
        }

        protected class ListLogger : ILogger
        {
            private readonly object _sync = new object();

            public ITestOutputHelper TestOutputHelper { get; set; }

            public List<(LogLevel, EventId, string, object, Exception)> LoggedEvents { get; }
                = new List<(LogLevel, EventId, string, object, Exception)>();

            public void Clear()
            {
                lock (_sync) // Guard against tests with explicit concurrency
                {
                    LoggedEvents.Clear();
                }
            }

            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                lock (_sync) // Guard against tests with explicit concurrency
                {
                    var message = formatter(state, exception)?.Trim();
                    if (message != null)
                    {
                        TestOutputHelper?.WriteLine(message + Environment.NewLine);
                    }

                    LoggedEvents.Add((logLevel, eventId, message, state, exception));
                }
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope(object state) => null;

            public IDisposable BeginScope<TState>(TState state) => null;
        }
    }
}
