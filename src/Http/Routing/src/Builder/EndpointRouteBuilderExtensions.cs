// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    internal const string MapEndpointTrimmerWarning = "This API may perform reflection on the supplied delegate and its parameters. These types may be trimmed if not directly referenced.";

    // Avoid creating a new array every call
    private static readonly string[] GetVerb = new[] { HttpMethods.Get };
    private static readonly string[] PostVerb = new[] { HttpMethods.Post };
    private static readonly string[] PutVerb = new[] { HttpMethods.Put };
    private static readonly string[] DeleteVerb = new[] { HttpMethods.Delete };
    private static readonly string[] PatchVerb = new[] { HttpMethods.Patch };

    /// <summary>
    /// Creates a <see cref="GroupRouteBuilder"/> for defining endpoints all prefixed with the specified <paramref name="prefix"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the group to.</param>
    /// <param name="prefix">The pattern that prefixes all routes in this group.</param>
    /// <returns>
    /// A <see cref="GroupRouteBuilder"/> that is both an <see cref="IEndpointRouteBuilder"/> and an <see cref="IEndpointConventionBuilder"/>.
    /// The same builder can be used to add endpoints with the given <paramref name="prefix"/>, and to customize those endpoints using conventions.
    /// </returns>
    public static GroupRouteBuilder MapGroup(this IEndpointRouteBuilder endpoints, string prefix) =>
        endpoints.MapGroup(RoutePatternFactory.Parse(prefix ?? throw new ArgumentNullException(nameof(prefix))));

    /// <summary>
    /// Creates a <see cref="GroupRouteBuilder"/> for defining endpoints all prefixed with the specified <paramref name="prefix"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the group to.</param>
    /// <param name="prefix">The pattern that prefixes all routes in this group.</param>
    /// <returns>
    /// A <see cref="GroupRouteBuilder"/> that is both an <see cref="IEndpointRouteBuilder"/> and an <see cref="IEndpointConventionBuilder"/>.
    /// The same builder can be used to add endpoints with the given <paramref name="prefix"/>, and to customize those endpoints using conventions.
    /// </returns>
    public static GroupRouteBuilder MapGroup(this IEndpointRouteBuilder endpoints, RoutePattern prefix)
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
    [RequiresUnreferencedCode(EndpointRouteBuilderExtensions.MapEndpointTrimmerWarning)]
    public static IEndpointConventionBuilder MapGet(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        RequestDelegate requestDelegate)
    {
        var returnType = requestDelegate.Method.ReturnType;
        if (returnType is { IsGenericType: true } && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return MapMethods(endpoints, pattern, GetVerb, requestDelegate as Delegate);
        }
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
        string pattern,
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
        string pattern,
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
        string pattern,
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
        string pattern,
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
       string pattern,
       IEnumerable<string> httpMethods,
       RequestDelegate requestDelegate)
    {
        ArgumentNullException.ThrowIfNull(httpMethods);

        var builder = endpoints.Map(RoutePatternFactory.Parse(pattern), requestDelegate);
        builder.WithDisplayName($"{pattern} HTTP: {string.Join(", ", httpMethods)}");
        builder.WithMetadata(new HttpMethodMetadata(httpMethods));
        return builder;
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
        string pattern,
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
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(requestDelegate);

        const int defaultOrder = 0;

        var builder = new RouteEndpointBuilder(
            requestDelegate,
            pattern,
            defaultOrder)
        {
            DisplayName = pattern.RawText ?? pattern.DebuggerToString(),
        };

        // Add delegate attributes as metadata
        var attributes = requestDelegate.Method.GetCustomAttributes();

        // This can be null if the delegate is a dynamic method or compiled from an expression tree
        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                builder.Metadata.Add(attribute);
            }
        }

        var dataSource = endpoints.DataSources.OfType<ModelEndpointDataSource>().FirstOrDefault();
        if (dataSource == null)
        {
            dataSource = new ModelEndpointDataSource();
            endpoints.DataSources.Add(dataSource);
        }

        return dataSource.AddEndpointBuilder(builder);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP GET requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder MapGet(
        this IEndpointRouteBuilder endpoints,
        string pattern,
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
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder MapPost(
        this IEndpointRouteBuilder endpoints,
        string pattern,
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
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder MapPut(
        this IEndpointRouteBuilder endpoints,
        string pattern,
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
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder MapDelete(
        this IEndpointRouteBuilder endpoints,
        string pattern,
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
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder MapPatch(
        this IEndpointRouteBuilder endpoints,
        string pattern,
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
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder MapMethods(
       this IEndpointRouteBuilder endpoints,
       string pattern,
       IEnumerable<string> httpMethods,
       Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(httpMethods);

        var disableInferredBody = false;
        foreach (var method in httpMethods)
        {
            disableInferredBody = ShouldDisableInferredBody(method);
            if (disableInferredBody is true)
            {
                break;
            }
        }

        var initialMetadata = new object[] { new HttpMethodMetadata(httpMethods) };
        var builder = endpoints.Map(RoutePatternFactory.Parse(pattern), handler, disableInferredBody, initialMetadata);

        // Prepends the HTTP method to the DisplayName produced with pattern + method name
        builder.Add(b => b.DisplayName = $"HTTP: {string.Join(", ", httpMethods)} {b.DisplayName}");

        return builder;

        static bool ShouldDisableInferredBody(string method)
        {
            // GET, DELETE, HEAD, CONNECT, TRACE, and OPTIONS normally do not contain bodies
            return method.Equals(HttpMethods.Get, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Delete, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Head, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Options, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Trace, StringComparison.Ordinal) ||
                   method.Equals(HttpMethods.Connect, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder Map(
        this IEndpointRouteBuilder endpoints,
        string pattern,
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
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder Map(
        this IEndpointRouteBuilder endpoints,
        RoutePattern pattern,
        Delegate handler)
    {
        return Map(endpoints, pattern, handler, disableInferBodyFromParameters: false);
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
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder MapFallback(this IEndpointRouteBuilder endpoints, Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(handler);

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
    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder MapFallback(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        var conventionBuilder = endpoints.Map(pattern, handler);
        conventionBuilder.WithDisplayName("Fallback " + pattern);
        conventionBuilder.Add(b => ((RouteEndpointBuilder)b).Order = int.MaxValue);
        return conventionBuilder;
    }

    [RequiresUnreferencedCode(MapEndpointTrimmerWarning)]
    private static RouteHandlerBuilder Map(
        this IEndpointRouteBuilder endpoints,
        RoutePattern pattern,
        Delegate handler,
        bool disableInferBodyFromParameters,
        IEnumerable<object>? initialEndpointMetadata = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        const int defaultOrder = 0;

        var fullPattern = pattern;

        if (endpoints is GroupRouteBuilder group)
        {
            fullPattern = RoutePatternFactory.Combine(group.GroupPrefix, pattern);
        }

        var builder = new RouteEndpointBuilder(
            pattern,
            defaultOrder)
        {
            DisplayName = fullPattern.RawText ?? fullPattern.DebuggerToString(),
            ServiceProvider = endpoints.ServiceProvider,
        };

        // Methods defined in a top-level program are generated as statics so the delegate
        // target will be null. Inline lambdas are compiler generated method so they can
        // be filtered that way.
        if (GeneratedNameParser.TryParseLocalFunctionName(handler.Method.Name, out var endpointName)
            || !TypeHelper.IsCompilerGeneratedMethod(handler.Method))
        {
            endpointName ??= handler.Method.Name;
            builder.DisplayName = $"{builder.DisplayName} => {endpointName}";
        }

        var dataSource = endpoints.DataSources.OfType<ModelEndpointDataSource>().FirstOrDefault();
        if (dataSource is null)
        {
            dataSource = new ModelEndpointDataSource();
            endpoints.DataSources.Add(dataSource);
        }

        var routeHandlerBuilder = new RouteHandlerBuilder(dataSource.AddEndpointBuilder(builder));
        routeHandlerBuilder.Add(RouteHandlerBuilderConvention);

        [UnconditionalSuppressMessage("Trimmer", "IL2026", Justification = "We surface a RequireUnreferencedCode in the call to enclosing Map method. " +
            "The trimmer is unable to infer this on the nested lambda.")]
        void RouteHandlerBuilderConvention(EndpointBuilder endpointBuilder)
        {
            var routeParams = new List<string>(fullPattern.Parameters.Count);
            foreach (var part in fullPattern.Parameters)
            {
                routeParams.Add(part.Name);
            }

            var routeHandlerOptions = endpoints.ServiceProvider?.GetService<IOptions<RouteHandlerOptions>>();
            var options = new RequestDelegateFactoryOptions
            {
                ServiceProvider = endpoints.ServiceProvider,
                RouteParameterNames = routeParams,
                ThrowOnBadRequest = routeHandlerOptions?.Value.ThrowOnBadRequest ?? false,
                DisableInferBodyFromParameters = disableInferBodyFromParameters,
                RouteHandlerFilterFactories = routeHandlerBuilder.RouteHandlerFilterFactories,
                InitialEndpointMetadata = initialEndpointMetadata
            };
            var filteredRequestDelegateResult = RequestDelegateFactory.Create(handler, options);

            // Add request delegate metadata
            foreach (var metadata in filteredRequestDelegateResult.EndpointMetadata)
            {
                endpointBuilder.Metadata.Add(metadata);
            }

            endpointBuilder.RequestDelegate = filteredRequestDelegateResult.RequestDelegate;
        }

        return routeHandlerBuilder;
    }
}
