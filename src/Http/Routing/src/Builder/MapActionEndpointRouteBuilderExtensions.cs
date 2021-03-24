// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to define HTTP API endpoints.
    /// </summary>
    public static class MapActionEndpointRouteBuilderExtensions
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
        public static MapActionEndpointConventionBuilder MapGet(
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
        public static MapActionEndpointConventionBuilder MapPost(
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
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that canaction be used to further customize the endpoint.</returns>
        public static MapActionEndpointConventionBuilder MapPut(
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
        public static MapActionEndpointConventionBuilder MapDelete(
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
        public static MapActionEndpointConventionBuilder MapMethods(
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
        public static MapActionEndpointConventionBuilder Map(
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
        public static MapActionEndpointConventionBuilder Map(
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

            var builder = new RouteEndpointBuilder(
                RequestDelegateBuilder.BuildRequestDelegate(action),
                pattern,
                defaultOrder)
            {
                DisplayName = pattern.RawText ?? pattern.DebuggerToString(),
            };

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

            return new MapActionEndpointConventionBuilder(dataSource.AddEndpointBuilder(builder));
        }
    }
}
