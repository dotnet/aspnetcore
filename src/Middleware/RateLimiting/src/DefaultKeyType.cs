// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

internal struct DefaultKeyType
{
    public DefaultKeyType(string policyName, object? key)
    {
        PolicyName = policyName;
        Key = key;
    }

    public string PolicyName { get; }

    public object? Key { get; }
}
