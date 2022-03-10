// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal sealed class CreatedAtRouteHttpResult : ObjectAtRouteHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedAtRouteHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public CreatedAtRouteHttpResult(object? routeValues, object? value)
        : this(routeName: null, routeValues: routeValues, value: value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedAtRouteHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public CreatedAtRouteHttpResult(
        string? routeName,
        object? routeValues,
        object? value)
        : base(routeName, routeValues, value, StatusCodes.Status201Created)
    {
    }
}
