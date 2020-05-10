// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Microsoft.AspNetCore.Routing
{
    internal class EndpointMiddlewareEventSource : EventSource
    {
        public static readonly EndpointMiddlewareEventSource Log = new EndpointMiddlewareEventSource();

        private readonly ConcurrentDictionary<string, EndpointCounter> _endpointCounters = new ConcurrentDictionary<string, EndpointCounter>();

        internal EndpointMiddlewareEventSource()
            : base("Microsoft.AspNetCore.Routing")
        {

        }

        [Event(1, Level = EventLevel.Informational)]
        public void ExecutingEndpoint(string endpoint)
        {
            var endpointCounter = _endpointCounters.GetOrAdd(endpoint, key => new EndpointCounter(key, this));
            endpointCounter.Executing();
            WriteEvent(3, endpoint);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void ExecutedEndpoint(string endpoint)
        {
            var endpointCounter = _endpointCounters.GetOrAdd(endpoint, key => new EndpointCounter(key, this));
            endpointCounter.Executed();
            WriteEvent(2, endpoint);
        }

        [Event(3, Level = EventLevel.Error)]
        public void FailedEndpoint(string endpoint)
        {
            var endpointCounter = _endpointCounters.GetOrAdd(endpoint, key => new EndpointCounter(key, this));
            endpointCounter.Failed();
            WriteEvent(3, endpoint);
        }


        private class EndpointCounter
        {
            private readonly IncrementingPollingCounter _requestsPerSecondCounter;
            private readonly PollingCounter _totalRequestsCounter;
            private readonly PollingCounter _failedRequestsCounter;
            private readonly PollingCounter _currentRequestsCounter;

            private long _totalRequests;
            private long _currentRequests;
            private long _failedRequests;

            public EndpointCounter(string endpoint, EventSource eventSource)
            {
                _requestsPerSecondCounter = new IncrementingPollingCounter($"{endpoint}:requests-per-second", eventSource, () => _totalRequests)
                {
                    DisplayName = $"{endpoint}:Request Rate",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                _totalRequestsCounter = new PollingCounter($"{endpoint}:total-requests", eventSource, () => _totalRequests)
                {
                    DisplayName = $"{endpoint}:Total Requests",
                };

                _currentRequestsCounter = new PollingCounter($"{endpoint}:current-requests", eventSource, () => _currentRequests)
                {
                    DisplayName = $"{endpoint}:Current Requests"
                };

                _failedRequestsCounter = new PollingCounter($"{endpoint}:failed-requests", eventSource, () => _failedRequests)
                {
                    DisplayName = $"{endpoint}:Failed Requests"
                };

            }

            public void Executing()
            {
                Interlocked.Increment(ref _totalRequests);
                Interlocked.Increment(ref _currentRequests);
            }

            public void Executed()
            {
                Interlocked.Decrement(ref _currentRequests);
            }

            public void Failed()
            {
                Interlocked.Increment(ref _failedRequests);
            }
        }
    }
}
