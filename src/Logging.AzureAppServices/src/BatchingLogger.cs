// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Extensions.Logging.AzureAppServices;

internal sealed class BatchingLogger : ILogger
{
    private readonly BatchingLoggerProvider _provider;
    private readonly string _category;

    public BatchingLogger(BatchingLoggerProvider loggerProvider, string categoryName)
    {
        _provider = loggerProvider;
        _category = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _provider.IsEnabled;
    }

    public void Log<TState>(DateTimeOffset timestamp, LogLevel logLevel, EventId _, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var builder = new StringBuilder();
        builder.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture));
        builder.Append(" [");
        builder.Append(logLevel.ToString());
        builder.Append("] ");
        builder.Append(_category);

        var scopeProvider = _provider.ScopeProvider;
        if (scopeProvider != null)
        {
            scopeProvider.ForEachScope((scope, stringBuilder) =>
            {
                stringBuilder.Append(" => ").Append(scope);
            }, builder);

            builder.AppendLine(":");
        }
        else
        {
            builder.Append(": ");
        }

        builder.AppendLine(formatter(state, exception));

        if (exception != null)
        {
            builder.AppendLine(exception.ToString());
        }

        _provider.AddMessage(timestamp, builder.ToString());
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        Log(DateTimeOffset.Now, logLevel, eventId, state, exception, formatter);
    }
}
