// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Routing;

internal sealed class RoutingMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.Routing";

    private readonly Meter _meter;
    private readonly Counter<long> _matchSuccessCounter;
    private readonly Counter<long> _matchFailureCounter;

    public RoutingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _matchSuccessCounter = _meter.CreateCounter<long>(
           "routing-match-success",
            description: "Number of requests that successfully matched to an endpoint.");

        _matchFailureCounter = _meter.CreateCounter<long>(
           "routing-match-failure",
            description: "Number of requests that failed to match to an endpoint. An unmatched request may be handled by later middleware, such as the static files or authentication middleware.");
    }

    public bool MatchSuccessCounterEnabled => _matchSuccessCounter.Enabled;

    public void MatchSuccess(string route, bool isFallback)
    {
        _matchSuccessCounter.Add(1,
            new KeyValuePair<string, object?>("route", route),
            new KeyValuePair<string, object?>("fallback", isFallback));
    }

    public void MatchFailure()
    {
        _matchFailureCounter.Add(1);
    }
}
