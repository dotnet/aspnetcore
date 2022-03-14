// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
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
        RouteValues = routeValues == null ? null : new RouteValueDictionary(routeValues);
    }

    /// <summary>
    /// Gets or sets the object result.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets or sets the name of the route to use for generating the URL.
    /// </summary>
    public string? RouteName { get; }

    /// <summary>
    /// Gets or sets the route data to use for generating the URL.
    /// </summary>
    public RouteValueDictionary? RouteValues { get; }

    /// <summary>
    /// 
    /// </summary>
    public int? StatusCode => StatusCodes.Status201Created;

    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, Value, statusCode: StatusCode, responseHeader: ConfigureResponseHeaders);

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
