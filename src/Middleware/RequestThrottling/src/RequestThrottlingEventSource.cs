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
            : base("Microsoft.AspNetCore.RequestThrottling")
        {
        }

        [Event(1, Level = EventLevel.Warning)]
        public void RequestRejected()
        {
            WriteEvent(1);
            Interlocked.Increment(ref _rejectedRequests);
        }

        [NonEvent]
        public QueueFrame QueueTimer()
        {
            Interlocked.Increment(ref _queueLength);

            if (IsEnabled())
            {
                return new QueueFrame
                {
                    _timer = ValueStopwatch.StartNew()
                };
            }
            return default;
        }

        internal struct QueueFrame : IDisposable
        {
            internal ValueStopwatch _timer;

            public void Dispose()
            {
                Interlocked.Decrement(ref Log._queueLength);

                if (Log.IsEnabled())
                {
                    var duration = _timer.IsActive ? _timer.GetElapsedTime().TotalMilliseconds : 0.0;
                    Log._queueDuration.WriteMetric(duration);
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
                    DisplayName = "Average Queue Duration",
                };
            }
        }

        // two functions for unit tests, hopefully there's a better pattern
        // put these in an extensions file in the test folder?
        internal int QueuedRequests => _queueLength;

        internal void Reset()
        {
            _queueLength = 0;
        }
    }
}
