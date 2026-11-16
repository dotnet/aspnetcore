// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

internal sealed record LogEntry(LogLevel Level, EventId EventId, Exception? Exception);

internal sealed class RecordingLoggerFactory : ILoggerFactory, ILogger
{
    public List<LogEntry> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => this;

    public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add(new LogEntry(logLevel, eventId, exception));

    public void Dispose()
    {
    }
}
