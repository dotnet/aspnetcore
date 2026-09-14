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
        "read-connections-paused",
        "write-connections-paused",
        "bytes-read",
        "bytes-written",
        "epoll-waits",
        "epoll-wakeups",
        "epoll-timeouts",
        "epoll-ready-events",
        "epoll-ready-batch-size"
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
    public void EmitsEpollEvents()
    {
        using var eventSource = new DirectTlsEventSource(
            $"{DirectTlsEventSource.EventSourceName}.{Guid.NewGuid():N}");
        using var listener = new TestEventListener(eventSource, level: EventLevel.Verbose);

        eventSource.RecordEpollConnectionRegistered(pumpId: 3, fileDescriptor: 42, "connection-id", events: 1);
        eventSource.RecordEpollInterestChanged(pumpId: 3, fileDescriptor: 42, "connection-id", previousEvents: 1, events: 4);
        eventSource.RecordEpollReady(pumpId: 3, fileDescriptor: 42, "connection-id", events: 5);
        eventSource.RecordEpollConnectionUnregistered(pumpId: 3, fileDescriptor: 42, "connection-id", events: 4);

        Assert.Collection(
            listener.Events,
            eventData => AssertEpollEvent(eventData, 3, "EpollConnectionRegistered", [3, 42, "connection-id", 1U]),
            eventData => AssertEpollEvent(eventData, 4, "EpollInterestChanged", [3, 42, "connection-id", 1U, 4U]),
            eventData => AssertEpollEvent(eventData, 5, "EpollReady", [3, 42, "connection-id", 5U]),
            eventData => AssertEpollEvent(eventData, 6, "EpollConnectionUnregistered", [3, 42, "connection-id", 4U]));
    }

    [Fact]
    public async Task ExposesEngineCounters()
    {
        using var eventSource = new DirectTlsEventSource(
            $"{DirectTlsEventSource.EventSourceName}.{Guid.NewGuid():N}");
        using var listener = new TestEventListener(eventSource, collectCounters: true);

        eventSource.ConnectionOwned(pumpId: 0, pumpConnectionCount: 1);
        eventSource.Accepted(pumpId: 0);
        eventSource.ReadConnectionPaused();
        eventSource.WriteConnectionPaused();
        eventSource.BytesRead(10);
        eventSource.BytesWritten(20);
        eventSource.EpollWaitCompleted(readyEventCount: 0);
        eventSource.EpollWaitCompleted(readyEventCount: 3);

        await listener.AllCountersObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(ExpectedCounterNames.Order(), listener.CounterNames.Order());

        eventSource.ReadConnectionResumed();
        eventSource.WriteConnectionResumed();
        eventSource.ConnectionReleased(pumpId: 0, pumpConnectionCount: 0);
    }

    private static void AssertEpollEvent(EventWrittenEventArgs eventData, int eventId, string eventName, object?[] payload)
    {
        Assert.Equal(eventId, eventData.EventId);
        Assert.Equal(eventName, eventData.EventName);
        Assert.Equal(payload, eventData.Payload);
    }

    private sealed class TestEventListener : EventListener
    {
        private readonly EventSource _eventSource;
        private readonly bool _collectCounters;
        private readonly ConcurrentQueue<EventWrittenEventArgs> _events = new();
        private readonly ConcurrentDictionary<string, byte> _counterNames = new();

        public TestEventListener(
            EventSource eventSource,
            bool collectCounters = false,
            EventLevel level = EventLevel.Informational)
        {
            _eventSource = eventSource;
            _collectCounters = collectCounters;
            EnableEvents(
                eventSource,
                level,
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
