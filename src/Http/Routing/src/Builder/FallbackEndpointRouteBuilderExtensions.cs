// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Contains extension methods for <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class FallbackEndpointRouteBuilderExtensions
{
    /// <summary>
    /// The default route pattern used by fallback routing. <c>{*path:nonfile}</c>
    /// </summary>
    public static readonly string DefaultPattern = "{*path:nonfile}";

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
    /// requests for non-file-names with the lowest possible priority.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallback(IEndpointRouteBuilder, RequestDelegate)"/> is intended to handle cases where URL path of
    /// the request does not contain a file name, and no other endpoint has matched. This is convenient for routing
    /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
    /// result in an HTTP 404.
    /// </para>
    /// <para>
    /// <see cref="MapFallback(IEndpointRouteBuilder, RequestDelegate)"/> registers an endpoint using the pattern
    /// <c>{*path:nonfile}</c>. The order of the registered endpoint will be <c>int.MaxValue</c>.
    /// </para>
    /// </remarks>
    public static IEndpointConventionBuilder MapFallback(this IEndpointRouteBuilder endpoints, RequestDelegate requestDelegate)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(requestDelegate);

        return endpoints.MapFallback("{*path:nonfile}", requestDelegate);
    }

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
    /// the provided pattern with the lowest possible priority.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallback(IEndpointRouteBuilder, string, RequestDelegate)"/> is intended to handle cases where no
    /// other endpoint has matched. This is convenient for routing requests to a SPA framework.
    /// </para>
    /// <para>
    /// The order of the registered endpoint will be <c>int.MaxValue</c>.
    /// </para>
    /// <para>
    /// This overload will use the provided <paramref name="pattern"/> verbatim. Use the <c>:nonfile</c> route constraint
    /// to exclude requests for static files.
    /// </para>
    /// </remarks>
    public static IEndpointConventionBuilder MapFallback(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        RequestDelegate requestDelegate)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(requestDelegate);

        var conventionBuilder = endpoints.Map(pattern, requestDelegate);
        conventionBuilder.WithDisplayName("Fallback " + pattern);
        conventionBuilder.Add(b => ((RouteEndpointBuilder)b).Order = int.MaxValue);
        conventionBuilder.WithMetadata(FallbackMetadata.Instance);
        return conventionBuilder;
    }
}
