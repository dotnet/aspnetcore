// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;

private class CounterListener : EventListener
{
    private readonly Dictionary<string, Channel<double>> _counters = new Dictionary<string, Channel<double>>();

    public CounterListener(string[] counterNames)
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