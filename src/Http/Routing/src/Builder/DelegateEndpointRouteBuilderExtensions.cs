// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to define HTTP API endpoints.
    /// </summary>
    public static class DelegateEndpointRouteBuilderExtensions
    {
        // Avoid creating a new array every call
        private static readonly string[] GetVerb = new[] { "GET" };
        private static readonly string[] PostVerb = new[] { "POST" };
        private static readonly string[] PutVerb = new[] { "PUT" };
        private static readonly string[] DeleteVerb = new[] { "DELETE" };

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP GET requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="handler">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder MapGet(
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
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder MapPost(
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
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder MapPut(
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
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder MapDelete(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate handler)
        {
            return MapMethods(endpoints, pattern, DeleteVerb, handler);
        }

        private static DelegateEndpointConventionBuilder Map(
            this IEndpointRouteBuilder endpoints,
            RoutePattern pattern,
            Delegate handler,
            bool disableInferredBody)
        {
            if (endpoints is null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            const int defaultOrder = 0;

            var routeParams = new List<string>(pattern.Parameters.Count);
            foreach (var part in pattern.Parameters)
            {
                routeParams.Add(part.Name);
            }

            var routeHandlerOptions = endpoints.ServiceProvider?.GetService<IOptions<RouteHandlerOptions>>();

            var options = new RequestDelegateFactoryOptions
            {
                ServiceProvider = endpoints.ServiceProvider,
                RouteParameterNames = routeParams,
                ThrowOnBadRequest = routeHandlerOptions?.Value.ThrowOnBadRequest ?? false,
                DisableInferredBody = disableInferredBody,
            };

            var requestDelegateResult = RequestDelegateFactory.Create(handler, options);

            var builder = new RouteEndpointBuilder(
                requestDelegateResult.RequestDelegate,
                pattern,
                defaultOrder)
            {
                DisplayName = pattern.RawText ?? pattern.DebuggerToString(),
            };

            // REVIEW: Should we add an IActionMethodMetadata with just MethodInfo on it so we are
            // explicit about the MethodInfo representing the "handler" and not the RequestDelegate?

            // Add MethodInfo as metadata to assist with OpenAPI generation for the endpoint.
            builder.Metadata.Add(handler.Method);

            // Methods defined in a top-level program are generated as statics so the delegate
            // target will be null. Inline lambdas are compiler generated method so they can
            // be filtered that way.
            if (GeneratedNameParser.TryParseLocalFunctionName(handler.Method.Name, out var endpointName)
                || !TypeHelper.IsCompilerGeneratedMethod(handler.Method))
            {
                endpointName ??= handler.Method.Name;

                builder.Metadata.Add(new EndpointNameMetadata(endpointName));
                builder.Metadata.Add(new RouteNameMetadata(endpointName));
                builder.DisplayName = $"{builder.DisplayName} => {endpointName}";
            }

            // Add delegate attributes as metadata
            var attributes = handler.Method.GetCustomAttributes();

            // Add add request delegate metadata 
            foreach (var metadata in requestDelegateResult.EndpointMetadata)
            {
                builder.Metadata.Add(metadata);
            }

            // This can be null if the delegate is a dynamic method or compiled from an expression tree
            if (attributes is not null)
            {
                foreach (var attribute in attributes)
                {
                    builder.Metadata.Add(attribute);
                }
            }

            var dataSource = endpoints.DataSources.OfType<ModelEndpointDataSource>().FirstOrDefault();
            if (dataSource is null)
            {
                dataSource = new ModelEndpointDataSource();
                endpoints.DataSources.Add(dataSource);
            }

            return new DelegateEndpointConventionBuilder(dataSource.AddEndpointBuilder(builder));
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified HTTP methods and pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="handler">The delegate executed when the endpoint is matched.</param>
        /// <param name="httpMethods">HTTP methods that the endpoint will match.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder MapMethods(
           this IEndpointRouteBuilder endpoints,
           string pattern,
           IEnumerable<string> httpMethods,
           Delegate handler)
        {
            if (httpMethods is null)
            {
                throw new ArgumentNullException(nameof(httpMethods));
            }

            var disableInferredBody = false;
            // GET, DELETE, HEAD, CONNECT, TRACE, and OPTIONS normally do not contain bodies
            if (!httpMethods.Any(method => !(method.Equals(HttpMethods.Get, StringComparison.Ordinal) ||
                method.Equals(HttpMethods.Delete, StringComparison.Ordinal) ||
                method.Equals(HttpMethods.Head, StringComparison.Ordinal) ||
                method.Equals(HttpMethods.Options, StringComparison.Ordinal) ||
                method.Equals(HttpMethods.Trace, StringComparison.Ordinal) ||
                method.Equals(HttpMethods.Connect, StringComparison.Ordinal))))
            {
                disableInferredBody = true;
            }

            var builder = endpoints.Map(RoutePatternFactory.Parse(pattern), handler, disableInferredBody);
            // Prepends the HTTP method to the DisplayName produced with pattern + method name
            builder.Add(b => b.DisplayName = $"HTTP: {string.Join(", ", httpMethods)} {b.DisplayName}");
            builder.WithMetadata(new HttpMethodMetadata(httpMethods));
            return builder;
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="handler">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder Map(
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
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static DelegateEndpointConventionBuilder Map(
            this IEndpointRouteBuilder endpoints,
            RoutePattern pattern,
            Delegate handler)
        {
            return Map(endpoints, pattern, handler, disableInferredBody: false);
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="handler">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
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
        public static DelegateEndpointConventionBuilder MapFallback(this IEndpointRouteBuilder endpoints, Delegate handler)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return endpoints.MapFallback("{*path:nonfile}", handler);
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// the provided pattern with the lowest possible priority.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="handler">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
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
        public static DelegateEndpointConventionBuilder MapFallback(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate handler)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var conventionBuilder = endpoints.Map(pattern, handler);
            conventionBuilder.WithDisplayName("Fallback " + pattern);
            conventionBuilder.Add(b => ((RouteEndpointBuilder)b).Order = int.MaxValue);
            return conventionBuilder;
        }
    }
}
