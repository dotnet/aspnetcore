// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.RateLimiting;
internal class DefaultKeyTypeEqualityComparer : IEqualityComparer<DefaultKeyType>
{
    public bool Equals(DefaultKeyType x, DefaultKeyType y)
    {
        var xKey = x.Key;
        var yKey = y.Key;
        if (xKey == null && yKey == null)
        {
            return true;
        }
        else if (xKey == null || yKey == null)
        {
            return false;
        }

        return x.PolicyName.Equals(y.PolicyName) && xKey.Equals(yKey);
    }

    public int GetHashCode([DisallowNull] DefaultKeyType obj)
    {
        return (obj.Key?.GetHashCode() ?? 0) + obj.PolicyName.GetHashCode();
    }
}
