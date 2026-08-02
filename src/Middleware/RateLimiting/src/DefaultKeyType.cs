// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

internal struct DefaultKeyType
{
    public DefaultKeyType(string? policyName, object? key, object? factory = null)
    {
        PolicyName = policyName;
        Key = key;
        Factory = factory;
    }

    public string? PolicyName { get; }

    public object? Key { get; }

    // This is really a Func<TPartitionKey, RateLimiter>
    public object? Factory { get; }
}
