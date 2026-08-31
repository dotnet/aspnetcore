// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

public class DirectTlsEventSourceTests
{
    private static readonly string[] ExpectedCounterNames =
    [
        "connections-owned",
        "accepts",
        "connections-paused",
        "bytes-read",
        "bytes-written"
    ];

    [Fact]
    public void EmitsPumpEvents()
    {
        using var eventSource = new DirectTlsEventSource(
            $"{DirectTlsEventSource.EventSourceName}.{Guid.NewGuid():N}");
        using var listener = new TestEventListener(eventSource);

        eventSource.Accepted(pumpId: 3);
        eventSource.ConnectionOwned(pumpId: 3, pumpConnectionCount: 1);
        eventSource.ConnectionReleased(pumpId: 3, pumpConnectionCount: 0);

        var eventSourceError = listener.Events
            .Where(eventData => eventData.EventName == "EventSourceMessage")
            .Select(eventData => eventData.Payload?[0]?.ToString())
            .FirstOrDefault();
        Assert.True(eventSourceError is null, eventSourceError);
        Assert.Collection(
            listener.Events,
            eventData =>
            {
                Assert.Equal(1, eventData.EventId);
                Assert.Equal("ConnectionAccepted", eventData.EventName);
                Assert.Equal(3, eventData.Payload![0]);
            },
            eventData =>
            {
                Assert.Equal(2, eventData.EventId);
                Assert.Equal("PumpConnections", eventData.EventName);
                Assert.Equal(3, eventData.Payload![0]);
                Assert.Equal(1, eventData.Payload[1]);
            },
            eventData =>
            {
                Assert.Equal(2, eventData.EventId);
                Assert.Equal("PumpConnections", eventData.EventName);
                Assert.Equal(3, eventData.Payload![0]);
                Assert.Equal(0, eventData.Payload[1]);
            });
    }

    [Fact]
    public async Task ExposesEngineCounters()
    {
        using var eventSource = new DirectTlsEventSource(
            $"{DirectTlsEventSource.EventSourceName}.{Guid.NewGuid():N}");
        using var listener = new TestEventListener(eventSource, collectCounters: true);

        eventSource.ConnectionOwned(pumpId: 0, pumpConnectionCount: 1);
        eventSource.Accepted(pumpId: 0);
        eventSource.ConnectionPaused();
        eventSource.BytesRead(10);
        eventSource.BytesWritten(20);

        await listener.AllCountersObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(ExpectedCounterNames.Order(), listener.CounterNames.Order());

        eventSource.ConnectionResumed();
        eventSource.ConnectionReleased(pumpId: 0, pumpConnectionCount: 0);
    }

    private sealed class TestEventListener : EventListener
    {
        private readonly EventSource _eventSource;
        private readonly bool _collectCounters;
        private readonly ConcurrentQueue<EventWrittenEventArgs> _events = new();
        private readonly ConcurrentDictionary<string, byte> _counterNames = new();

        public TestEventListener(EventSource eventSource, bool collectCounters = false)
        {
            _eventSource = eventSource;
            _collectCounters = collectCounters;
            EnableEvents(
                eventSource,
                EventLevel.Informational,
                EventKeywords.All,
                collectCounters
                    ? new Dictionary<string, string?> { ["EventCounterIntervalSec"] = "0.1" }
                    : null);
        }

        public EventWrittenEventArgs[] Events => _events.ToArray();

        public string[] CounterNames => _counterNames.Keys.ToArray();

        public TaskCompletionSource AllCountersObserved { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!ReferenceEquals(eventData.EventSource, _eventSource))
            {
                return;
            }

            if (eventData.EventName == "EventCounters" &&
                eventData.Payload?[0] is IDictionary<string, object> payload &&
                payload["Name"] is string counterName)
            {
                _counterNames.TryAdd(counterName, 0);
                if (_counterNames.Count == ExpectedCounterNames.Length)
                {
                    AllCountersObserved.TrySetResult();
                }
                return;
            }

            if (!_collectCounters)
            {
                _events.Enqueue(eventData);
            }
        }
    }
}
