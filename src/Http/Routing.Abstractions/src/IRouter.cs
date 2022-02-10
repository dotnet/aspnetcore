// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Interface for implementing a router.
/// </summary>
public interface IRouter
{
    /// <summary>
    /// Asynchronously routes based on the current <paramref name="context"/>.
    /// </summary>
    /// <param name="context">A <see cref="RouteContext"/> instance.</param>
    Task RouteAsync(RouteContext context);

    /// <summary>
    /// Returns the URL that is associated with the route details provided in <paramref name="context"/>
    /// </summary>
    /// <param name="context">A <see cref="VirtualPathContext"/> instance.</param>
    /// <returns>A <see cref="VirtualPathData"/> object. Can be null.</returns>
    VirtualPathData? GetVirtualPath(VirtualPathContext context);
}
