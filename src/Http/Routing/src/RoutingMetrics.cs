// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Routing;

internal sealed class RoutingMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.Routing";

    private readonly Meter _meter;
    private readonly Counter<long> _matchAttemptsCounter;

    public RoutingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _matchAttemptsCounter = _meter.CreateCounter<long>(
           "aspnet.routing.match_attempts",
            description: "Number of requests that were attempted to be matched to an endpoint.");
    }

    public bool MatchSuccessCounterEnabled => _matchAttemptsCounter.Enabled;

    public void MatchSuccess(string route, bool isFallback)
    {
        _matchAttemptsCounter.Add(1,
            new KeyValuePair<string, object?>("http.route", route),
            new KeyValuePair<string, object?>("aspnet.routing.match_status", "success"),
            new KeyValuePair<string, object?>("aspnet.routing.route.is_fallback", isFallback));
    }

    public void MatchFailure()
    {
        _matchAttemptsCounter.Add(1,
            new KeyValuePair<string, object?>("aspnet.routing.match_status", "failure"));
    }
}
