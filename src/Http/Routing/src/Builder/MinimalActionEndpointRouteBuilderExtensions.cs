// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to define HTTP API endpoints.
    /// </summary>
    public static class MinimalActionEndpointRouteBuilderExtensions
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
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder MapGet(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            return MapMethods(endpoints, pattern, GetVerb, action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP POST requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder MapPost(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            return MapMethods(endpoints, pattern, PostVerb, action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP PUT requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder MapPut(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            return MapMethods(endpoints, pattern, PutVerb, action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP DELETE requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder MapDelete(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            return MapMethods(endpoints, pattern, DeleteVerb, action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified HTTP methods and pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <param name="httpMethods">HTTP methods that the endpoint will match.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder MapMethods(
           this IEndpointRouteBuilder endpoints,
           string pattern,
           IEnumerable<string> httpMethods,
           Delegate action)
        {
            if (httpMethods is null)
            {
                throw new ArgumentNullException(nameof(httpMethods));
            }

            var builder = endpoints.Map(RoutePatternFactory.Parse(pattern), action);
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
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder Map(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            return Map(endpoints, RoutePatternFactory.Parse(pattern), action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MinimalActionEndpointConventionBuilder Map(
            this IEndpointRouteBuilder endpoints,
            RoutePattern pattern,
            Delegate action)
        {
            if (endpoints is null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            const int defaultOrder = 0;

            var routeParams = new List<string>(pattern.Parameters.Count);
            foreach (var part in pattern.Parameters)
            {
                routeParams.Add(part.Name);
            }

            var options = new RequestDelegateFactoryOptions
            {
                ServiceProvider = endpoints.ServiceProvider,
                RouteParameterNames = routeParams
            };

            var builder = new RouteEndpointBuilder(
                RequestDelegateFactory.Create(action, options),
                pattern,
                defaultOrder)
            {
                DisplayName = pattern.RawText ?? pattern.DebuggerToString(),
            };

            // REVIEW: Should we add an IActionMethodMetadata with just MethodInfo on it so we are
            // explicit about the MethodInfo representing the "action" and not the RequestDelegate?

            // Add MethodInfo as metadata to assist with OpenAPI generation for the endpoint.
            builder.Metadata.Add(action.Method);

            // Methods defined in a top-level program are generated as statics so the delegate
            // target will be null. Inline lambdas are compiler generated properties so they can
            // be filtered that way.
            if (action.Target == null || !TypeHelper.IsCompilerGenerated(action.Method.Name))
            {
                builder.Metadata.Add(new EndpointNameMetadata(action.Method.Name));
            }

            // Add delegate attributes as metadata
            var attributes = action.Method.GetCustomAttributes();

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

            return new MinimalActionEndpointConventionBuilder(dataSource.AddEndpointBuilder(builder));
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// requests for non-file-names with the lowest possible priority.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
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
        public static MinimalActionEndpointConventionBuilder MapFallback(this IEndpointRouteBuilder endpoints, Delegate action)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return endpoints.MapFallback("{*path:nonfile}", action);
        }

        /// <summary>
        /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
        /// the provided pattern with the lowest possible priority.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
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
        public static MinimalActionEndpointConventionBuilder MapFallback(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var conventionBuilder = endpoints.Map(pattern, action);
            conventionBuilder.WithDisplayName("Fallback " + pattern);
            conventionBuilder.Add(b => ((RouteEndpointBuilder)b).Order = int.MaxValue);
            return conventionBuilder;
        }
    }
}
