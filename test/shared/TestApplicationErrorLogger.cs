// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    public class TestApplicationErrorLogger : ILogger
    {
        // Application errors are logged using 13 as the eventId.
        private const int ApplicationErrorEventId = 13;

        public bool ThrowOnCriticalErrors { get; set; } = true;

        public ConcurrentQueue<LogMessage> Messages { get; } = new ConcurrentQueue<LogMessage>();

        public ConcurrentQueue<object> Scopes { get; } = new ConcurrentQueue<object>();

        public int TotalErrorsLogged => Messages.Count(message => message.LogLevel == LogLevel.Error);

        public int CriticalErrorsLogged => Messages.Count(message => message.LogLevel == LogLevel.Critical);

        public int ApplicationErrorsLogged => Messages.Count(message => message.EventId.Id == ApplicationErrorEventId);

        public IDisposable BeginScope<TState>(TState state)
        {
            Scopes.Enqueue(state);

            return new Disposable(() => { Scopes.TryDequeue(out _); });
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
#if true
            if (logLevel == LogLevel.Critical && ThrowOnCriticalErrors)
#endif
            {
                Console.WriteLine($"Log {logLevel}[{eventId}]: {formatter(state, exception)} {exception?.Message}");

                if (logLevel == LogLevel.Critical && ThrowOnCriticalErrors)
                {
                    throw new Exception("Unexpected critical error.", exception);
                }
            }

            Messages.Enqueue(new LogMessage
            {
                LogLevel = logLevel,
                EventId = eventId,
                Exception = exception,
                Message = formatter(state, exception)
            });
        }

        public class LogMessage
        {
            public LogLevel LogLevel { get; set; }
            public EventId EventId { get; set; }
            public Exception Exception { get; set; }
            public string Message { get; set; }
        }
    }
}
