// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    private const string MapEndpointUnreferencedCodeWarning = "This API may perform reflection on the supplied delegate and its parameters. These types may be trimmed if not directly referenced.";
    private const string MapEndpointDynamicCodeWarning = "This API may perform reflection on the supplied delegate and its parameters. These types may require generated code and aren't compatible with native AOT applications.";

    // Avoid creating a new array every call
    private static readonly string[] GetVerb = new[] { HttpMethods.Get };
    private static readonly string[] PostVerb = new[] { HttpMethods.Post };
    private static readonly string[] PutVerb = new[] { HttpMethods.Put };
    private static readonly string[] DeleteVerb = new[] { HttpMethods.Delete };
    private static readonly string[] PatchVerb = new[] { HttpMethods.Patch };

    /// <summary>
    /// Creates a <see cref="RouteGroupBuilder"/> for defining endpoints all prefixed with the specified <paramref name="prefix"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the group to.</param>
    /// <param name="prefix">The pattern that prefixes all routes in this group.</param>
    /// <returns>
    /// A <see cref="RouteGroupBuilder"/> that is both an <see cref="IEndpointRouteBuilder"/> and an <see cref="IEndpointConventionBuilder"/>.
    /// The same builder can be used to add endpoints with the given <paramref name="prefix"/>, and to customize those endpoints using conventions.
    /// </returns>
    public static RouteGroupBuilder MapGroup(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string prefix) =>
        endpoints.MapGroup(RoutePatternFactory.Parse(prefix ?? throw new ArgumentNullException(nameof(prefix))));

    /// <summary>
    /// Creates a <see cref="RouteGroupBuilder"/> for defining endpoints all prefixed with the specified <paramref name="prefix"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the group to.</param>
    /// <param name="prefix">The pattern that prefixes all routes in this group.</param>
    /// <returns>
    /// A <see cref="RouteGroupBuilder"/> that is both an <see cref="IEndpointRouteBuilder"/> and an <see cref="IEndpointConventionBuilder"/>.
    /// The same builder can be used to add endpoints with the given <paramref name="prefix"/>, and to customize those endpoints using conventions.
    /// </returns>
    public static RouteGroupBuilder MapGroup(this IEndpointRouteBuilder endpoints, RoutePattern prefix)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(prefix);

        return new(endpoints, prefix);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP GET requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapGet(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        RequestDelegate requestDelegate)
    {
        return MapMethods(endpoints, pattern, GetVerb, requestDelegate);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP POST requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapPost(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        RequestDelegate requestDelegate)
    {
        return MapMethods(endpoints, pattern, PostVerb, requestDelegate);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP PUT requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapPut(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        RequestDelegate requestDelegate)
    {
        return MapMethods(endpoints, pattern, PutVerb, requestDelegate);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP DELETE requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapDelete(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        RequestDelegate requestDelegate)
    {
        return MapMethods(endpoints, pattern, DeleteVerb, requestDelegate);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP PATCH requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapPatch(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        RequestDelegate requestDelegate)
    {
        return MapMethods(endpoints, pattern, PatchVerb, requestDelegate);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
    /// for the specified HTTP methods and pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <param name="httpMethods">HTTP methods that the endpoint will match.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapMethods(
       this IEndpointRouteBuilder endpoints,
       [StringSyntax("Route")] string pattern,
       IEnumerable<string> httpMethods,
       RequestDelegate requestDelegate)
    {
        ArgumentNullException.ThrowIfNull(httpMethods);

        return endpoints.Map(RoutePatternFactory.Parse(pattern), requestDelegate, httpMethods);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder Map(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        RequestDelegate requestDelegate)
    {
        return Map(endpoints, RoutePatternFactory.Parse(pattern), requestDelegate);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder Map(
        this IEndpointRouteBuilder endpoints,
        RoutePattern pattern,
        RequestDelegate requestDelegate)
    {
        return Map(endpoints, pattern, requestDelegate, httpMethods: null);
    }

    private static IEndpointConventionBuilder Map(
        this IEndpointRouteBuilder endpoints,
        RoutePattern pattern,
        RequestDelegate requestDelegate,
        IEnumerable<string>? httpMethods)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(requestDelegate);

        return endpoints
            .GetOrAddRouteEndpointDataSource()
            .AddRequestDelegate(pattern, requestDelegate, httpMethods, CreateHandlerRequestDelegate, requestDelegate.Method);

        static RequestDelegateResult CreateHandlerRequestDelegate(Delegate handler, RequestDelegateFactoryOptions options, RequestDelegateMetadataResult? metadataResult)
        {
            var requestDelegate = (RequestDelegate)handler;

            // Create request delegate that calls filter pipeline.
            if (options.EndpointBuilder?.FilterFactories.Count > 0)
            {
                requestDelegate = RequestDelegateFilterPipelineBuilder.Create(requestDelegate, options);
            }

            IReadOnlyList<object> metadata = options.EndpointBuilder?.Metadata is not null ?
                new List<object>(options.EndpointBuilder.Metadata) :
                Array.Empty<object>();
            return new RequestDelegateResult(requestDelegate, metadata);
        }
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP GET requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapGet(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return MapMethods(endpoints, pattern, GetVerb, handler);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP POST requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapPost(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return MapMethods(endpoints, pattern, PostVerb, handler);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP PUT requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapPut(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return MapMethods(endpoints, pattern, PutVerb, handler);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP DELETE requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapDelete(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return MapMethods(endpoints, pattern, DeleteVerb, handler);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP PATCH requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The <see cref="Delegate" /> executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapPatch(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return MapMethods(endpoints, pattern, PatchVerb, handler);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
    /// for the specified HTTP methods and pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <param name="httpMethods">HTTP methods that the endpoint will match.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapMethods(
       this IEndpointRouteBuilder endpoints,
       [StringSyntax("Route")] string pattern,
       IEnumerable<string> httpMethods,
       Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(httpMethods);
        return endpoints.Map(RoutePatternFactory.Parse(pattern), handler, httpMethods, isFallback: false);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder Map(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return Map(endpoints, RoutePatternFactory.Parse(pattern), handler);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder Map(
        this IEndpointRouteBuilder endpoints,
        RoutePattern pattern,
        Delegate handler)
    {
        return Map(endpoints, pattern, handler, httpMethods: null, isFallback: false);
    }

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
    /// requests for non-file-names with the lowest possible priority.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallback(IEndpointRouteBuilder, Delegate)"/> is intended to handle cases where URL path of
    /// the request does not contain a file name, and no other endpoint has matched. This is convenient for routing
    /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
    /// result in an HTTP 404.
    /// </para>
    /// <para>
    /// <see cref="MapFallback(IEndpointRouteBuilder, Delegate)"/> registers an endpoint using the pattern
    /// <c>{*path:nonfile}</c>. The order of the registered endpoint will be <c>int.MaxValue</c>.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapFallback(this IEndpointRouteBuilder endpoints, Delegate handler)
    {
        return endpoints.MapFallback("{*path:nonfile}", handler);
    }

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
    /// the provided pattern with the lowest possible priority.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallback(IEndpointRouteBuilder, string, Delegate)"/> is intended to handle cases where no
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
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder MapFallback(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler)
    {
        return endpoints.Map(RoutePatternFactory.Parse(pattern), handler, httpMethods: null, isFallback: true);
    }

    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    private static RouteHandlerBuilder Map(
        this IEndpointRouteBuilder endpoints,
        RoutePattern pattern,
        Delegate handler,
        IEnumerable<string>? httpMethods,
        bool isFallback)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        return endpoints
            .GetOrAddRouteEndpointDataSource()
            .AddRouteHandler(pattern, handler, httpMethods, isFallback, RequestDelegateFactory.InferMetadata, RequestDelegateFactory.Create, handler.Method);
    }

    internal static RouteEndpointDataSource GetOrAddRouteEndpointDataSource(this IEndpointRouteBuilder endpoints)
    {
        RouteEndpointDataSource? routeEndpointDataSource = null;

        foreach (var dataSource in endpoints.DataSources)
        {
            if (dataSource is RouteEndpointDataSource foundDataSource)
            {
                routeEndpointDataSource = foundDataSource;
                break;
            }
        }

        if (routeEndpointDataSource is null)
        {
            // ServiceProvider isn't nullable, but it is being called by methods that historically did not access this property, so we null check anyway.
            var routeHandlerOptions = endpoints.ServiceProvider?.GetService<IOptions<RouteHandlerOptions>>();
            var throwOnBadRequest = routeHandlerOptions?.Value.ThrowOnBadRequest ?? false;

            routeEndpointDataSource = new RouteEndpointDataSource(endpoints.ServiceProvider ?? EmptyServiceProvider.Instance, throwOnBadRequest);
            endpoints.DataSources.Add(routeEndpointDataSource);
        }

        return routeEndpointDataSource;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
