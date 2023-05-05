// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

internal readonly struct MetricsContext
{
    public readonly string? PolicyName;
    public readonly string? Method;
    public readonly string? Route;
    public readonly bool CurrentLeaseRequestsCounterEnabled;
    public readonly bool CurrentRequestsQueuedCounterEnabled;

    public MetricsContext(string? policyName, string? method, string? route, bool currentLeaseRequestsCounterEnabled, bool currentRequestsQueuedCounterEnabled)
    {
        PolicyName = policyName;
        Method = method;
        Route = route;
        CurrentLeaseRequestsCounterEnabled = currentLeaseRequestsCounterEnabled;
        CurrentRequestsQueuedCounterEnabled = currentRequestsQueuedCounterEnabled;
    }
}
