// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.FunctionalTests.Helpers
{
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
            private List<string> _messages = new List<string>();

            private object _sync = new object();

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
}
