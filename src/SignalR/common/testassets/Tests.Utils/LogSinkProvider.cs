// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    // TestSink does not have an event
    internal class LogSinkProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogRecord> _logs = new ConcurrentQueue<LogRecord>();

        public event Action<LogRecord> RecordLogged;

        public ILogger CreateLogger(string categoryName)
        {
            return new LogSinkLogger(categoryName, this);
        }

        public void Dispose()
        {
        }

        public IList<LogRecord> GetLogs() => _logs.ToList();

        public void Log<TState>(string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var record = new LogRecord(
                DateTime.Now,
                new WriteContext
                {
                    LoggerName = categoryName,
                    LogLevel = logLevel,
                    EventId = eventId,
                    State = state,
                    Exception = exception,
                    Formatter = (o, e) => formatter((TState)o, e),
                });
            _logs.Enqueue(record);

            RecordLogged?.Invoke(record);
        }

        private class LogSinkLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly LogSinkProvider _logSinkProvider;

            public LogSinkLogger(string categoryName, LogSinkProvider logSinkProvider)
            {
                _categoryName = categoryName;
                _logSinkProvider = logSinkProvider;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _logSinkProvider.Log(_categoryName, logLevel, eventId, state, exception, formatter);
            }
        }
    }
}