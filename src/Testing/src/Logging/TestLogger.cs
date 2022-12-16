// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Testing;

public class TestLogger : ILogger
{
    private object _scope;
    private readonly ITestSink _sink;
    private readonly string _name;
    private readonly Func<LogLevel, bool> _filter;

    public TestLogger(string name, ITestSink sink, bool enabled)
        : this(name, sink, _ => enabled)
    {
    }

    public TestLogger(string name, ITestSink sink, Func<LogLevel, bool> filter)
    {
        _sink = sink;
        _name = name;
        _filter = filter;
    }

    public string Name { get; set; }

    public IDisposable BeginScope<TState>(TState state)
    {
        _scope = state;

        _sink.Begin(new BeginScopeContext()
        {
            LoggerName = _name,
            Scope = state,
        });

        return TestDisposable.Instance;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        _sink.Write(new WriteContext()
        {
            LogLevel = logLevel,
            EventId = eventId,
            State = state,
            Exception = exception,
            Formatter = (s, e) => formatter((TState)s, e),
            LoggerName = _name,
            Scope = _scope
        });
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && _filter(logLevel);
    }

    private sealed class TestDisposable : IDisposable
    {
        public static readonly TestDisposable Instance = new TestDisposable();

        public void Dispose()
        {
            // intentionally does nothing
        }
    }
}
