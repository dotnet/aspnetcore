// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.RequestThrottling
{
    internal sealed class RequestThrottlingEventSource : EventSource
    {
        public static readonly RequestThrottlingEventSource Log = new RequestThrottlingEventSource();

        private PollingCounter _rejectedRequestsCounter;   // incrementing polling counter
        private PollingCounter _queueLengthCounter;        // polling counter
        private EventCounter _queueDuration;     // event counter average

        private long _rejectedRequests;
        private int _queueLength;

        internal RequestThrottlingEventSource()
            : base("Microsoft.AspNet.RequestThrottling")
        {
        }

        [Event(1, Level = EventLevel.Warning)]
        public void RequestRejected()
        {
            Interlocked.Increment(ref _rejectedRequests);
            WriteEvent(1);
        }

        [NonEvent]
        public ValueStopwatch RequestEnqueued()
        {
            Interlocked.Increment(ref _queueLength);

            if (IsEnabled())
            {
                return ValueStopwatch.StartNew();
            }
            return default;
        }

        [NonEvent]
        public void RequestDequeued(ValueStopwatch timer)
        {
            Interlocked.Decrement(ref _queueLength);

            if (IsEnabled())
            {
                var duration = timer.IsActive ? timer.GetElapsedTime().TotalMilliseconds : 0.0;
                _queueDuration.WriteMetric(duration);
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

                _queueLengthCounter ??= new PollingCounter("queued-length", this, () => _queueLength)
                {
                    DisplayName = "Queue Length",
                };

                _queueDuration ??= new EventCounter("queue-duration", this)
                {
                    DisplayName = "Average Queue Duration",
                };
            }
        }

        // two functions for unit tests, hopefully there's a better pattern
        internal int QueuedRequests => _queueLength;

        internal void Reset()
        {
            _queueLength = 0;
        }
    }
}
