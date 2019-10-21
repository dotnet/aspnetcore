// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.AspNetCore.Hosting
{
    internal sealed class HostingEventSource : EventSource
    {
        public static readonly HostingEventSource Log = new HostingEventSource();

        private IncrementingPollingCounter _requestsPerSecondCounter;
        private PollingCounter _totalRequestsCounter;
        private PollingCounter _failedRequestsCounter;
        private PollingCounter _currentRequestsCounter;

        private long _totalRequests;
        private long _currentRequests;
        private long _failedRequests;

        internal HostingEventSource()
            : this("Microsoft.AspNetCore.Hosting")
        {

        }

        // Used for testing
        internal HostingEventSource(string eventSourceName)
            : base(eventSourceName)
        {
        }

        // NOTE
        // - The 'Start' and 'Stop' suffixes on the following event names have special meaning in EventSource. They
        //   enable creating 'activities'.
        //   For more information, take a look at the following blog post:
        //   https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/
        // - A stop event's event id must be next one after its start event.

        [Event(1, Level = EventLevel.Informational)]
        public void HostStart()
        {
            WriteEvent(1);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void HostStop()
        {
            WriteEvent(2);
        }

        [Event(3, Level = EventLevel.Informational)]
        public void RequestStart(string method, string path)
        {
            Interlocked.Increment(ref _totalRequests);
            Interlocked.Increment(ref _currentRequests);
            WriteEvent(3, method, path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(4, Level = EventLevel.Informational)]
        public void RequestStop()
        {
            Interlocked.Decrement(ref _currentRequests);
            WriteEvent(4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(5, Level = EventLevel.Error)]
        public void UnhandledException()
        {
            WriteEvent(5);
        }

        internal void RequestFailed()
        {
            Interlocked.Increment(ref _failedRequests);
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).
                // They aren't disabled afterwards...

                _requestsPerSecondCounter ??= new IncrementingPollingCounter("requests-per-second", this, () => _totalRequests)
                {
                    DisplayName = "Request Rate",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                _totalRequestsCounter ??= new PollingCounter("total-requests", this, () => _totalRequests)
                {
                    DisplayName = "Total Requests",
                };

                _currentRequestsCounter ??= new PollingCounter("current-requests", this, () => _currentRequests)
                {
                    DisplayName = "Current Requests"
                };

                _failedRequestsCounter ??= new PollingCounter("failed-requests", this, () => _failedRequests)
                {
                    DisplayName = "Failed Requests"
                };
            }
        }
    }
}
