// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.ConcurrencyLimiter;

internal sealed class ConcurrencyLimiterEventSource : EventSource
{
    public static readonly ConcurrencyLimiterEventSource Log = new ConcurrencyLimiterEventSource();
    private static readonly QueueFrame CachedNonTimerResult = new QueueFrame(timer: null, parent: Log);

#pragma warning disable IDE0052 // Remove unread private members (2021-02-02: These ARE set in OnEventCommand - the the IDE0052 analyzer is incorrect at this time)
    private PollingCounter? _rejectedRequestsCounter;
    private PollingCounter? _queueLengthCounter;
#pragma warning restore IDE0052 // Remove unread private members

    private EventCounter? _queueDuration;

    private long _rejectedRequests;
    private int _queueLength;

    internal ConcurrencyLimiterEventSource()
        : base("Microsoft.AspNetCore.ConcurrencyLimiter")
    {
    }

    // Used for testing
    internal ConcurrencyLimiterEventSource(string eventSourceName)
        : base(eventSourceName, EventSourceSettings.EtwManifestEventFormat)
    {
    }

    [Event(1, Level = EventLevel.Warning)]
    public void RequestRejected()
    {
        Interlocked.Increment(ref _rejectedRequests);
        WriteEvent(1);
    }

    [NonEvent]
    public void QueueSkipped()
    {
        if (IsEnabled())
        {
            _queueDuration!.WriteMetric(0);
        }
    }

    [NonEvent]
    public QueueFrame QueueTimer()
    {
        Interlocked.Increment(ref _queueLength);

        if (IsEnabled())
        {
            return new QueueFrame(ValueStopwatch.StartNew(), this);
        }

        return CachedNonTimerResult;
    }

    internal struct QueueFrame : IDisposable
    {
        private readonly ValueStopwatch? _timer;
        private readonly ConcurrencyLimiterEventSource _parent;

        public QueueFrame(ValueStopwatch? timer, ConcurrencyLimiterEventSource parent)
        {
            _timer = timer;
            _parent = parent;
        }

        public void Dispose()
        {
            Interlocked.Decrement(ref _parent._queueLength);

            if (_parent.IsEnabled() && _timer != null)
            {
                var duration = _timer.Value.GetElapsedTime().TotalMilliseconds;
                _parent._queueDuration!.WriteMetric(duration);
            }
        }
    }

    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        if (command.Command == EventCommand.Enable)
        {
            _rejectedRequestsCounter ??= new PollingCounter("requests-rejected", this, () => Volatile.Read(ref _rejectedRequests))
            {
                DisplayName = "Rejected Requests",
            };

            _queueLengthCounter ??= new PollingCounter("queue-length", this, () => Volatile.Read(ref _queueLength))
            {
                DisplayName = "Queue Length",
            };

            _queueDuration ??= new EventCounter("queue-duration", this)
            {
                DisplayName = "Average Time in Queue",
            };
        }
    }
}
