// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Internal;

public sealed class HttpEventSourceListener : EventListener
{
    private readonly StringBuilder _messageBuilder = new StringBuilder();
    private readonly ILogger _logger;
    private readonly object _lock = new object();
    private bool _disposed;

    public HttpEventSourceListener(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(HttpEventSourceListener));
        _logger.LogDebug($"Starting {nameof(HttpEventSourceListener)}.");
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        base.OnEventSourceCreated(eventSource);

        if (IsHttpEventSource(eventSource))
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
                }
            }
        }
    }

    private static bool IsHttpEventSource(EventSource eventSource)
    {
        return eventSource.Name.Contains("System.Net.Quic") || eventSource.Name.Contains("System.Net.Http");
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        base.OnEventWritten(eventData);

        if (!IsHttpEventSource(eventData.EventSource))
        {
            return;
        }

        string message;
        lock (_messageBuilder)
        {
            _messageBuilder.Append("<- Event ");
            _messageBuilder.Append(eventData.EventSource.Name);
            _messageBuilder.Append(" - ");
            _messageBuilder.Append(eventData.EventName);
            _messageBuilder.Append(" : ");
            _messageBuilder.AppendJoin(',', eventData.Payload!);
            _messageBuilder.Append(" ->");
            message = _messageBuilder.ToString();
            _messageBuilder.Clear();
        }

        // We don't know the state of the logger after dispose.
        // Ensure that any messages written in the background aren't
        // logged after the listener has been disposed in the test.
        lock (_lock)
        {
            if (!_disposed)
            {
                // EventListener base constructor subscribes to events.
                // It is possible to start getting events before the
                // super constructor is run and logger is assigned.
                _logger?.LogDebug(message);
            }
        }
    }

    public override string ToString()
    {
        return _messageBuilder.ToString();
    }

    public override void Dispose()
    {
        base.Dispose();

        lock (_lock)
        {
            if (!_disposed)
            {
                _logger?.LogDebug($"Stopping {nameof(HttpEventSourceListener)}.");
                _disposed = true;
            }
        }
    }
}
