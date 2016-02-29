// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class TestApplicationErrorLogger : ILogger
    {
        // Application errors are logged using 13 as the eventId.
        private const int ApplicationErrorEventId = 13;

        public int ApplicationErrorsLogged { get; set; }

        public IDisposable BeginScopeImpl(object state)
        {
            return new Disposable(() => { });
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (eventId.Id == ApplicationErrorEventId)
            {
                ApplicationErrorsLogged++;
            }
        }
    }
}
