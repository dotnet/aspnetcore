// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    public class TestApplicationErrorLogger : ILogger
    {
        // Application errors are logged using 13 as the eventId.
        private const int ApplicationErrorEventId = 13;

        public int TotalErrorsLogged { get; set; }

        public int CriticalErrorsLogged { get; set; }

        public int ApplicationErrorsLogged { get; set; }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new Disposable(() => { });
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
#if false
            Console.WriteLine($"Log {logLevel}[{eventId}]: {formatter(state, exception)} {exception?.Message}");
#endif

            if (eventId.Id == ApplicationErrorEventId)
            {
                ApplicationErrorsLogged++;
            }

            if (logLevel == LogLevel.Error)
            {
                TotalErrorsLogged++;
            }

            if (logLevel == LogLevel.Critical)
            {
                CriticalErrorsLogged++;
            }
        }
    }
}
