// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with status code Created (201) and Location header.
/// Targets a registered route.
/// </summary>
/// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
public sealed class CreatedAtRoute<TValue> : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedAtRoute"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to format in the entity body.</param>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
    internal CreatedAtRoute(object? routeValues, TValue? value)
        : this(routeName: null, routeValues: routeValues, value: value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedAtRoute"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to format in the entity body.</param>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
    internal CreatedAtRoute(string? routeName, object? routeValues, TValue? value)
        : this(routeName, new RouteValueDictionary(routeValues), value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedAtRoute"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to format in the entity body.</param>
    internal CreatedAtRoute(
        string? routeName,
        RouteValueDictionary? routeValues,
        TValue? value)
    {
        Value = value;
        RouteName = routeName;
        RouteValues = routeValues ?? new RouteValueDictionary();
        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);
    }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public TValue? Value { get; }

    object? IValueHttpResult.Value => Value;

    /// <summary>
    /// Gets the name of the route to use for generating the URL.
    /// </summary>
    public string? RouteName { get; }

    /// <summary>
    /// Gets the route data to use for generating the URL.
    /// </summary>
    public RouteValueDictionary RouteValues { get; }

    /// <summary>
    /// Gets the HTTP status code: <see cref="StatusCodes.Status201Created"/>
    /// </summary>
    public int StatusCode => StatusCodes.Status201Created;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

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

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.CreatedAtRouteResult");

        httpContext.Response.Headers.Location = url;

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);
        httpContext.Response.StatusCode = StatusCode;

        return HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger,
                Value);
    }

    /// <inheritdoc/>
    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(ProducesResponseTypeMetadata.CreateUnvalidated(typeof(TValue), StatusCodes.Status201Created, ContentTypeConstants.ApplicationJsonContentTypes));
    }
}
