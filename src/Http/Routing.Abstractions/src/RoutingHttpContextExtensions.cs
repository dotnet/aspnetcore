// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Extension methods for <see cref="HttpContext"/> related to routing.
/// </summary>
public static class RoutingHttpContextExtensions
{
    /// <summary>
    /// Gets the <see cref="RouteData"/> associated with the provided <paramref name="httpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <returns>The <see cref="RouteData"/>.</returns>
    public static RouteData GetRouteData(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var routingFeature = httpContext.Features.Get<IRoutingFeature>();
        return routingFeature?.RouteData ?? new RouteData(httpContext.Request.RouteValues);
    }

    /// <summary>
    /// Gets a route value from <see cref="RouteData.Values"/> associated with the provided
    /// <paramref name="httpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="key">The key of the route value.</param>
    /// <returns>The corresponding route value, or null.</returns>
    public static object? GetRouteValue(this HttpContext httpContext, string key)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(key);

        return httpContext.Features.Get<IRouteValuesFeature>()?.RouteValues[key];
    }
}
