// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.InternalTesting;

public class MockLogger : ILogger
{
    private readonly List<string> _messages = new List<string>();

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
