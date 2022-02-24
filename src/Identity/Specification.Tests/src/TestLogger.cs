// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity.Test;

/// <summary>
/// test logger.
/// </summary>
public interface ITestLogger
{
    /// <summary>
    /// log messages.
    /// </summary>
    IList<string> LogMessages { get; }
}

/// <summary>
/// Test logger.
/// </summary>
/// <typeparam name="TName"></typeparam>
public class TestLogger<TName> : ILogger<TName>, ITestLogger
{
    /// <summary>
    /// log messages.
    /// </summary>
    public IList<string> LogMessages { get; } = new List<string>();

    /// <summary>
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="state"></param>
    /// <returns></returns>
    public IDisposable BeginScope<TState>(TState state)
    {
        LogMessages.Add(state?.ToString());
        return null;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="logLevel"></param>
    /// <returns></returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="logLevel"></param>
    /// <param name="eventId"></param>
    /// <param name="state"></param>
    /// <param name="exception"></param>
    /// <param name="formatter"></param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (formatter == null)
        {
            LogMessages.Add(state.ToString());
        }
        else
        {
            LogMessages.Add(formatter(state, exception));
        }
    }
}
