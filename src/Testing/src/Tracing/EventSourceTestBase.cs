// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting.Tracing;

// This collection attribute is what makes the "magic" happen. It forces xunit to run all tests that inherit from this
// base class sequentially, preventing conflicts (since EventSource/EventListener is a process-global concept).
[Collection(CollectionName)]
public abstract class EventSourceTestBase : IDisposable
{
    public const string CollectionName = "Microsoft.AspNetCore.InternalTesting.Tracing.EventSourceTestCollection";

    private readonly CollectingEventListener _listener;

    public EventSourceTestBase()
    {
        _listener = new CollectingEventListener();
    }

    protected void CollectFrom(string eventSourceName)
    {
        _listener.CollectFrom(eventSourceName);
    }

    protected void CollectFrom(EventSource eventSource)
    {
        _listener.CollectFrom(eventSource);
    }

    protected IReadOnlyList<EventWrittenEventArgs> GetEvents() => _listener.GetEventsWritten();

    public void Dispose()
    {
        _listener.Dispose();
    }
}
