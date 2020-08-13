// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    internal sealed class ConcurrencyLimiterEventSource : EventSource
    {
        public static readonly ConcurrencyLimiterEventSource Log = new ConcurrencyLimiterEventSource();
        private static readonly QueueFrame CachedNonTimerResult = new QueueFrame(timer: null, parent: Log);

        private PollingCounter _rejectedRequestsCounter;
        private PollingCounter _queueLengthCounter;
        private EventCounter _queueDuration;

        private long _rejectedRequests;
        private int _queueLength;

        internal ConcurrencyLimiterEventSource()
            : base("Microsoft.AspNetCore.ConcurrencyLimiter")
        {
        }

        // Used for testing
        internal ConcurrencyLimiterEventSource(string eventSourceName)
            : base(eventSourceName)
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
                _queueDuration.WriteMetric(0);
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
            private ValueStopwatch? _timer;
            private ConcurrencyLimiterEventSource _parent;

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
                    _parent._queueDuration.WriteMetric(duration);
                }
            }
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                _rejectedRequestsCounter ??= new PollingCounter("requests-rejected", this, () => _rejectedRequests)
                {
                    DisplayName = "Rejected Requests",
                };

                _queueLengthCounter ??= new PollingCounter("queue-length", this, () => _queueLength)
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
}
