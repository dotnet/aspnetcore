// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Diagnostics;

// There are multiple instances of this metrics type. Each middleware creates an instance because there isn't a central place to register it in DI.
// Multiple instances is ok because the meter and counter names are consistent.
// DefaultMeterFactory caches instances meters by name, and meter caches instances of counters by name.
internal sealed class DiagnosticsMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.Diagnostics";

    private readonly Meter _meter;
    private readonly Counter<long> _handlerExceptionCounter;

    public DiagnosticsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _handlerExceptionCounter = _meter.CreateCounter<long>(
           "diagnostics-handler-exception",
            description: "Number of exceptions caught by exception handling middleware.");
    }

    public void RequestException(string exceptionName, ExceptionResult result, string? handler)
    {
        if (_handlerExceptionCounter.Enabled)
        {
            RequestExceptionCore(exceptionName, result, handler);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RequestExceptionCore(string exceptionName, ExceptionResult result, string? handler)
    {
        var tags = new TagList();
        tags.Add("exception-name", exceptionName);
        tags.Add("result", result.ToString());
        if (handler != null)
        {
            tags.Add("handler", handler);
        }
        _handlerExceptionCounter.Add(1, tags);
    }
}

internal enum ExceptionResult
{
    Skipped,
    Handled,
    Unhandled,
    Aborted
}
