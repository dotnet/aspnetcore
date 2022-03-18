// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with status code Accepted (202) and Location header.
/// Targets a registered route.
/// </summary>
public sealed class AcceptedAtRouteHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedAtRouteHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public AcceptedAtRouteHttpResult(object? routeValues, object? value)
        : this(routeName: null, routeValues: routeValues, value: value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedAtRouteHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public AcceptedAtRouteHttpResult(
        string? routeName,
        object? routeValues,
        object? value)
    {
        Value = value;
        RouteName = routeName;
        RouteValues = new RouteValueDictionary(routeValues);
    }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the name of the route to use for generating the URL.
    /// </summary>
    public string? RouteName { get; }

    /// <summary>
    /// Gets the route data to use for generating the URL.
    /// </summary>
    public RouteValueDictionary? RouteValues { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => StatusCodes.Status202Accepted;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var linkGenerator = httpContext.RequestServices.GetRequiredService<LinkGenerator>();
        var url = linkGenerator.GetUriByRouteValues(
            httpContext,
            RouteName,
            RouteValues,
            fragment: FragmentString.Empty);

        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("No route matches the supplied values.");
        }

        httpContext.Response.Headers.Location = url;
        return HttpResultsWriter.WriteResultAsJsonAsync(httpContext, Value, StatusCode);
    }
}
