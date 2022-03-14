// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with status code Created (201) and Location header.
/// Targets a registered route.
/// </summary>
public sealed class CreatedAtRouteHttpResult : IResult, IObjectHttpResult, IAtRouteHttpResult, IStatusCodeHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedAtRouteHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to format in the entity body.</param>
    internal CreatedAtRouteHttpResult(object? routeValues, object? value)
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
    internal CreatedAtRouteHttpResult(
        string? routeName,
        object? routeValues,
        object? value)
    {
        Value = value;
        RouteName = routeName;
        RouteValues = new RouteValueDictionary(routeValues);
    }

    /// <inheritdoc/>
    public object? Value { get; }

    /// <inheritdoc/>
    public string? RouteName { get; }

    /// <inheritdoc/>
    public RouteValueDictionary? RouteValues { get; }

    /// <inheritdoc/>
    public int? StatusCode => StatusCodes.Status201Created;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(
            httpContext,
            objectHttpResult: this,
            configureResponseHeader: ConfigureResponseHeaders);

    private void ConfigureResponseHeaders(HttpContext context)
    {
        var linkGenerator = context.RequestServices.GetRequiredService<LinkGenerator>();
        var url = linkGenerator.GetUriByRouteValues(
            context,
            RouteName,
            RouteValues,
            fragment: FragmentString.Empty);

        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("No route matches the supplied values.");
        }

        context.Response.Headers.Location = url;
    }
}
