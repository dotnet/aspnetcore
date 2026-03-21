// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Internal;

internal sealed class CounterValues(string counterName, IAsyncEnumerator<double> values)
{
    public string CounterName { get; } = counterName;
    public IAsyncEnumerator<double> Values { get; } = values;
}

internal sealed class TestCounterListener : EventListener
{
    private readonly Dictionary<string, Channel<double>> _counters = new Dictionary<string, Channel<double>>();
    private readonly ILogger _logger;
    private readonly string _eventSourceName;

    /// <summary>
    /// Creates a new TestCounterListener.
    /// </summary>
    /// <param name="counterNames">The names of ALL counters for the event source. You must name each counter, even if you do not intend to use it.</param>
    public TestCounterListener(ILoggerFactory loggerFactory, string eventSourceName, string[] counterNames)
    {
        _logger = loggerFactory.CreateLogger<TestCounterListener>();
        foreach (var item in counterNames)
        {
            _counters[item] = Channel.CreateUnbounded<double>();
        }

        _eventSourceName = eventSourceName;
    }

    public CounterValues GetCounterValues(string counterName, CancellationToken cancellationToken = default)
    {
        var values = _counters[counterName].Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        return new CounterValues(counterName, values);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // Work around https://github.com/dotnet/runtime/issues/31927
        if (eventData.EventSource.Name == _eventSourceName && eventData.EventName == "EventCounters")
        {
            var payload = (IDictionary<string, object>)eventData.Payload[0];
            var counter = (string)payload["Name"];
            if (payload.TryGetValue("Increment", out var increment))
            {
                _logger.LogDebug("Counter {CounterName} on event source {EventSourceName} has increment value {Value}.", counter, eventData.EventSource.Name, increment);
            }
            if (payload.TryGetValue("Mean", out var mean))
            {
                _logger.LogDebug("Counter {CounterName} on event source {EventSourceName} has mean value {Value}.", counter, eventData.EventSource.Name, mean);
            }

            var value = (double)(increment ?? mean);
            var writer = _counters[counter].Writer;
            writer.TryWrite(value);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (var item in _counters)
        {
            item.Value.Writer.TryComplete();
        }
    }
}
