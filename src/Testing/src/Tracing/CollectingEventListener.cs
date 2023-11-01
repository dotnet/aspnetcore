// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Microsoft.AspNetCore.InternalTesting.Tracing;

public class CollectingEventListener : EventListener
{
    private readonly ConcurrentQueue<EventWrittenEventArgs> _events = new ConcurrentQueue<EventWrittenEventArgs>();

    private readonly object _lock = new object();

    private readonly Dictionary<string, EventSource> _existingSources = new Dictionary<string, EventSource>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _requestedEventSources = new HashSet<string>();

    public void CollectFrom(string eventSourceName)
    {
        lock (_lock)
        {
            // Check if it's already been created
            if (_existingSources.TryGetValue(eventSourceName, out var existingSource))
            {
                // It has, so just enable it now
                CollectFrom(existingSource);
            }
            else
            {
                // It hasn't, so queue this request for when it is created
                _requestedEventSources.Add(eventSourceName);
            }
        }
    }

    public void CollectFrom(EventSource eventSource) => EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);

    public IReadOnlyList<EventWrittenEventArgs> GetEventsWritten() => _events.ToArray();

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        lock (_lock)
        {
            // Add this to the list of existing sources for future CollectEventsFrom requests.
            _existingSources[eventSource.Name] = eventSource;

            // Check if we have a pending request to enable it
            if (_requestedEventSources.Contains(eventSource.Name))
            {
                CollectFrom(eventSource);
            }
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        _events.Enqueue(eventData);
    }
}
