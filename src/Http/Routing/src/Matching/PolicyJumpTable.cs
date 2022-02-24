// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// Supports retrieving endpoints that fulfill a certain matcher policy.
/// </summary>
public abstract class PolicyJumpTable
{
    /// <summary>
    /// Returns the destination for a given <paramref name="httpContext"/> in the current jump table.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    public abstract int GetDestination(HttpContext httpContext);

    internal virtual string DebuggerToString()
    {
        return GetType().Name;
    }
}
