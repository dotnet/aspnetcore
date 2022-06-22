// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.RateLimiting;
internal class AspNetKeyEqualityComparer : IEqualityComparer<AspNetKey>
{
    public bool Equals(AspNetKey? x, AspNetKey? y)
    {
        if (x == null && y == null)
        {
            return true;
        }
        else if (x == null || y == null)
        {
            return false;
        }
        else if (x is AspNetKey<object> && y is AspNetKey<object>)
        {
            return ((AspNetKey<object>)x).Key.Equals(((AspNetKey<object>)y).Key);
        }
        else
        {
            return false;
        }
    }

    public int GetHashCode([DisallowNull] AspNetKey obj)
    {
        if (obj is AspNetKey<object>)
        {
            return ((AspNetKey<object>)obj).Key.GetHashCode();
        }
        // REVIEW - what's reasonable here? AspNetKey is a completely empty object
        else
        {
            return default;
        }
    }
}
