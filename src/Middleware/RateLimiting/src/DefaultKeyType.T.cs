// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;
internal sealed class DefaultKeyType<TKey>: DefaultKeyType
{
    private readonly TKey _key;

    public DefaultKeyType(TKey key)
    {
        _key = key;
    }

    public override object? GetKey()
    {
        return _key;
    }
}
