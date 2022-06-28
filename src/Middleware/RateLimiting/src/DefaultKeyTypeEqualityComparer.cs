// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.RateLimiting;
internal class DefaultKeyTypeEqualityComparer : IEqualityComparer<DefaultKeyType>
{
    public bool Equals(DefaultKeyType? x, DefaultKeyType? y)
    {
        if (x == null && y == null)
        {
            return true;
        }
        else if (x == null || y == null)
        {
            return false;
        }

        var xKey = x.GetKey();
        var yKey = y.GetKey();
        if (xKey == null && yKey == null)
        {
            return true;
        }
        else if (xKey == null || yKey == null)
        {
            return false;
        }

        return xKey.Equals(yKey);
    }

    public int GetHashCode([DisallowNull] DefaultKeyType obj)
    {
        var key = obj.GetKey();
        if (key is not null)
        {
            return key.GetHashCode();
        }
        // REVIEW - is this reasonable?
        return default;
    }
}
