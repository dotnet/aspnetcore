// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Provides RouteData
/// </summary>
public class DefaultRouteDataProvider
{
    /// <summary>
    /// Gets RouteData
    /// </summary>
    public virtual RouteData? GetRouteData(string url)
    {
        return null;
    }
}
