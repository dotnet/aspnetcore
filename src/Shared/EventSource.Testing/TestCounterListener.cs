// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;

namespace Microsoft.AspNetCore.Internal
{
    internal class TestCounterListener : EventListener
    {
        private readonly Dictionary<string, Channel<double>> _counters = new Dictionary<string, Channel<double>>();

        /// <summary>
        /// Creates a new TestCounterListener.
        /// </summary>
        /// <param name="counterNames">The names of ALL counters for the event source. You must name each counter, even if you do not intend to use it.</param>
        public TestCounterListener(string[] counterNames)
        {
            foreach (var item in counterNames)
            {
                _counters[item] = Channel.CreateUnbounded<double>();
            }
        }

        public IAsyncEnumerable<double> GetCounterValues(string counterName, CancellationToken cancellationToken = default)
        {
            return _counters[counterName].Reader.ReadAllAsync(cancellationToken);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName == "EventCounters")
            {
                var payload = (IDictionary<string, object>)eventData.Payload[0];
                var counter = (string)payload["Name"];
                payload.TryGetValue("Increment", out var increment);
                payload.TryGetValue("Mean", out var mean);
                var writer = _counters[counter].Writer;
                writer.TryWrite((double)(increment ?? mean));
            }
        }
    }
}
