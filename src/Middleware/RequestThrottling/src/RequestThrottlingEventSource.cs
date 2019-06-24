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
        private static QueueFrame _cachedNonTimerResult = new QueueFrame
        {
            _parent = Log
        };

        private PollingCounter _rejectedRequestsCounter;   // incrementing polling counter
        private PollingCounter _queueLengthCounter;        // polling counter
        private EventCounter _queueDuration;     // event counter average

        internal long _rejectedRequests;
        internal int _queueLength;

    
        internal RequestThrottlingEventSource()
            : base("Microsoft.AspNetCore.RequestThrottling")
        {
        }

        // Used for testing
        internal RequestThrottlingEventSource(string eventSourceName)
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
        public QueueFrame QueueTimer()
        {
            Interlocked.Increment(ref _queueLength);

            if (IsEnabled())
            {
                return new QueueFrame
                {
                    _timer = ValueStopwatch.StartNew(),
                    _parent = this
                };
            }

            return _cachedNonTimerResult;
        }

        internal struct QueueFrame : IDisposable
        {
            internal ValueStopwatch _timer;
            internal RequestThrottlingEventSource _parent;

            public void Dispose()
            {
                Interlocked.Decrement(ref _parent._queueLength);

                if (_parent.IsEnabled())
                {
                    var duration = _timer.IsActive ? _timer.GetElapsedTime().TotalMilliseconds : 0.0;
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
                    DisplayName = "Average Queue Duration",
                };
            }
        }
     }
}
