// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    public class MockLogger : ILogger
    {
        private List<string> _messages = new List<string>();

        public IDisposable BeginScope<TState>(TState state)
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _messages.Add(formatter(state, exception));
        }

        public IReadOnlyList<string> Messages => _messages;
    }
}
