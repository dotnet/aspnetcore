// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

internal readonly struct MetricsContext
{
    public readonly string? PolicyName;
    public readonly bool CurrentLeasedRequestsCounterEnabled;
    public readonly bool CurrentQueuedRequestsCounterEnabled;

    public MetricsContext(string? policyName, bool currentLeasedRequestsCounterEnabled, bool currentQueuedRequestsCounterEnabled)
    {
        PolicyName = policyName;
        CurrentLeasedRequestsCounterEnabled = currentLeasedRequestsCounterEnabled;
        CurrentQueuedRequestsCounterEnabled = currentQueuedRequestsCounterEnabled;
    }
}
