// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    public class TestApplicationErrorLogger : ILogger
    {
        // Application errors are logged using 13 as the eventId.
        private const int ApplicationErrorEventId = 13;

        public List<Type> IgnoredExceptions { get; } = new List<Type>();

        public bool ThrowOnCriticalErrors { get; set; } = true;

        public bool ThrowOnUngracefulShutdown { get; set; } = true;

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
            var exceptionIsIgnored = IgnoredExceptions.Contains(exception?.GetType());

            if (logLevel == LogLevel.Critical && ThrowOnCriticalErrors && !exceptionIsIgnored)
            {
                var log = $"Log {logLevel}[{eventId}]: {formatter(state, exception)} {exception}";

                Console.WriteLine(log);

                if (logLevel == LogLevel.Critical && ThrowOnCriticalErrors && !exceptionIsIgnored)
                {
                    throw new Exception($"Unexpected critical error. {log}", exception);
                }
            }

            // Fail tests where not all the connections close during server shutdown.
            if (ThrowOnUngracefulShutdown &&
                ((eventId.Id == 16 && eventId.Name == nameof(KestrelTrace.NotAllConnectionsClosedGracefully)) ||
                 (eventId.Id == 21 && eventId.Name == nameof(KestrelTrace.NotAllConnectionsAborted))))
            {
                var log = $"Log {logLevel}[{eventId}]: {formatter(state, exception)} {exception}";
                throw new Exception($"Shutdown failure. {log}");
            }

            // We don't use nameof here because this is logged by the transports and we don't know which one is
            // referenced in this shared source file.
            if (eventId.Id == 14 && eventId.Name == "ConnectionError")
            {
                var log = $"Log {logLevel}[{eventId}]: {formatter(state, exception)} {exception}";
                throw new Exception($"Unexpected connection error. {log}");
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
