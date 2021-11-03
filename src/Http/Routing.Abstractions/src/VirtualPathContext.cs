// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// A context for virtual path generation operations.
/// </summary>
public class VirtualPathContext
{
    /// <summary>
    /// Creates a new instance of <see cref="VirtualPathContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="Http.HttpContext"/> associated with the current request.</param>
    /// <param name="ambientValues">The set of route values associated with the current request.</param>
    /// <param name="values">The set of new values provided for virtual path generation.</param>
    public VirtualPathContext(
        HttpContext httpContext,
        RouteValueDictionary ambientValues,
        RouteValueDictionary values)
        : this(httpContext, ambientValues, values, null)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="VirtualPathContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="Http.HttpContext"/> associated with the current request.</param>
    /// <param name="ambientValues">The set of route values associated with the current request.</param>
    /// <param name="values">The set of new values provided for virtual path generation.</param>
    /// <param name="routeName">The name of the route to use for virtual path generation.</param>
    public VirtualPathContext(
        HttpContext httpContext,
        RouteValueDictionary ambientValues,
        RouteValueDictionary values,
        string? routeName)
    {
        HttpContext = httpContext;
        AmbientValues = ambientValues;
        Values = values;
        RouteName = routeName;
    }

    /// <summary>
    /// Gets the set of route values associated with the current request.
    /// </summary>
    public RouteValueDictionary AmbientValues { get; }

    /// <summary>
    /// Gets the <see cref="Http.HttpContext"/> associated with the current request.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// Gets the name of the route to use for virtual path generation.
    /// </summary>
    public string? RouteName { get; }

    /// <summary>
    /// Gets or sets the set of new values provided for virtual path generation.
    /// </summary>
    public RouteValueDictionary Values { get; set; }
}
