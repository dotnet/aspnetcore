// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace Microsoft.AspNetCore.Internal;

internal sealed class TestEventListener : EventListener
{
    private readonly int _eventId;

    public TestEventListener(int eventId)
    {
        _eventId = eventId;
    }

    public EventWrittenEventArgs EventData { get; private set; }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // The tests here run in parallel, capture the EventData that a test is explicitly
        // looking for and not give back other tests' data.
        if (eventData.EventId == _eventId)
        {
            EventData = eventData;
        }
    }
}
