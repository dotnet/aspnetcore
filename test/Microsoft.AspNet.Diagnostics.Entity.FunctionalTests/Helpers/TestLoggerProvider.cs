// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Logging;
using System.Collections.Generic;

namespace Microsoft.AspNet.Diagnostics.Entity.FunctionalTests.Helpers
{
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly TestLogger _logger = new TestLogger();

        public TestLogger Logger
        {
            get { return _logger; }
        }

        public ILogger Create(string name)
        {
            return _logger;
        }

        public class TestLogger : ILogger
        {
            private List<string> _messages = new List<string>();

            public IEnumerable<string> Messages
            {
                get { return _messages; }
            }

            public void Write(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                _messages.Add(formatter(state, exception));
            }
            
            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope(object state)
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