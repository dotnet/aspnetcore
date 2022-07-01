// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

internal sealed class DefaultKeyType<TKey> : DefaultKeyType
{
    private readonly TKey _key;

    public DefaultKeyType(string policyName, TKey key)
    {
        PolicyName = policyName;
        _key = key;
    }

    public override string PolicyName { get; }

    public override object? GetKey()
    {
        return _key;
    }
}
