// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides methods used for invoking the route endpoint
/// infrastructure with custom funcs for populating metadata
/// and creating request delegates. Intended to be consumed from
/// the RequestDeleatgeGenerator only.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RouteHandlerServices
{
    /// <summary>
    /// Registers an endpoint with custom functions for constructing
    /// a request delegate for its handler and populating metadata for
    /// the endpoint. Intended for consumption in the RequestDelegateGenerator.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <param name="httpMethods">The set of supported HTTP methods.</param>
    /// <param name="populateMetadata">A delegate for populating endpoint metadata.</param>
    /// <param name="createRequestDelegate">A delegate for constructing a RequestDelegate.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/>>.</returns>
    public static RouteHandlerBuilder Map(
            IEndpointRouteBuilder endpoints,
            [StringSyntax("Route")] string pattern,
            Delegate handler,
            IEnumerable<string>? httpMethods,
            Func<MethodInfo, RequestDelegateFactoryOptions?, RequestDelegateMetadataResult> populateMetadata,
            Func<Delegate, RequestDelegateFactoryOptions, RequestDelegateMetadataResult?, RequestDelegateResult> createRequestDelegate)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(populateMetadata);
        ArgumentNullException.ThrowIfNull(createRequestDelegate);

        return Map(endpoints, pattern, handler, httpMethods, populateMetadata, createRequestDelegate, handler.Method);
    }

    /// <summary>
    /// Registers an endpoint with custom functions for constructing
    /// a request delegate for its handler and populating metadata for
    /// the endpoint. Intended for consumption in the RequestDelegateGenerator.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <param name="httpMethods">The set of supported HTTP methods.</param>
    /// <param name="populateMetadata">A delegate for populating endpoint metadata.</param>
    /// <param name="createRequestDelegate">A delegate for constructing a RequestDelegate.</param>
    /// <param name="methodInfo">The MethodInfo associated with the incoming delegate.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/>>.</returns>
    public static RouteHandlerBuilder Map(
            IEndpointRouteBuilder endpoints,
            [StringSyntax("Route")] string pattern,
            Delegate handler,
            IEnumerable<string>? httpMethods,
            Func<MethodInfo, RequestDelegateFactoryOptions?, RequestDelegateMetadataResult> populateMetadata,
            Func<Delegate, RequestDelegateFactoryOptions, RequestDelegateMetadataResult?, RequestDelegateResult> createRequestDelegate,
            MethodInfo methodInfo)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(populateMetadata);
        ArgumentNullException.ThrowIfNull(createRequestDelegate);

        return endpoints
              .GetOrAddRouteEndpointDataSource()
              .AddRouteHandler(RoutePatternFactory.Parse(pattern),
                               handler,
                               httpMethods,
                               isFallback: false,
                               populateMetadata,
                               createRequestDelegate,
                               methodInfo);
    }
}
