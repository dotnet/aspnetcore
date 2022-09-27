// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class TagHelperLogger<T> : ILogger<T>
{
    public List<LoggerData> Logged { get; } = new List<LoggerData>();

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
        Logged.Add(new LoggerData(logLevel, state));
    }

    public class LoggerData
    {
        public LoggerData(LogLevel logLevel, object state)
        {
            LogLevel = logLevel;
            State = state;
        }

        public LogLevel LogLevel { get; set; }
        public object State { get; set; }
    }
}
