// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
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
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches the pattern specified via attributes.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>An <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static MapActionEndpointConventionBuilder MapAction(
            this IEndpointRouteBuilder endpoints,
            Delegate action)
        {
            if (endpoints is null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var requestDelegate = MapActionExpressionTreeBuilder.BuildRequestDelegate(action);

            var routeAttributes = action.Method.GetCustomAttributes().OfType<IRoutePatternMetadata>();
            var conventionBuilders = new List<IEndpointConventionBuilder>();

            const int defaultOrder = 0;

            foreach (var routeAttribute in routeAttributes)
            {
                if (routeAttribute.RoutePattern is not string pattern)
                {
                    continue;
                }

                var routeName = (routeAttribute as IRouteNameMetadata)?.RouteName;
                var routeOrder = (routeAttribute as IRouteOrderMetadata)?.RouteOrder;

                var conventionBuilder = endpoints.Map(pattern, requestDelegate);

                conventionBuilder.Add(endpointBuilder =>
                {
                    foreach (var attribute in action.Method.GetCustomAttributes())
                    {
                        endpointBuilder.Metadata.Add(attribute);
                    }

                    endpointBuilder.DisplayName = routeName ?? pattern;

                    ((RouteEndpointBuilder)endpointBuilder).Order = routeOrder ?? defaultOrder;
                });

                conventionBuilders.Add(conventionBuilder);
            }

            if (conventionBuilders.Count == 0)
            {
                throw new InvalidOperationException("Action must have a pattern. Is it missing a Route attribute?");
            }

            return new MapActionEndpointConventionBuilder(conventionBuilders);
        }

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

            var displayName = $"{pattern} HTTP: {string.Join(", ", httpMethods)}";
            var builder = endpoints.Map(RoutePatternFactory.Parse(pattern), action, displayName);
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
            return Map(endpoints, pattern, action, displayName: null);
        }

        private static MapActionEndpointConventionBuilder Map(
            this IEndpointRouteBuilder endpoints,
            RoutePattern pattern,
            Delegate action,
            string? displayName)
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
                MapActionExpressionTreeBuilder.BuildRequestDelegate(action),
                pattern,
                defaultOrder)
            {
                DisplayName = pattern.RawText ?? pattern.DebuggerToString(),
            };

            // Add delegate attributes as metadata
            var attributes = action.Method.GetCustomAttributes();
            string? routeName = null;
            int? routeOrder = null;

            // This can be null if the delegate is a dynamic method or compiled from an expression tree
            if (attributes is not null)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute is IRoutePatternMetadata patternMetadata && patternMetadata.RoutePattern is not null)
                    {
                        throw new InvalidOperationException($"'{attribute.GetType()}' implements {nameof(IRoutePatternMetadata)} which is not supported by this method.");
                    }
                    if (attribute is IHttpMethodMetadata methodMetadata && methodMetadata.HttpMethods.Any())
                    {
                        throw new InvalidOperationException($"'{attribute.GetType()}' implements {nameof(IHttpMethodMetadata)} which is not supported by this method.");
                    }

                    if (attribute is IRouteNameMetadata nameMetadata && nameMetadata.RouteName is string name)
                    {
                        routeName = name;
                    }
                    if (attribute is IRouteOrderMetadata orderMetadata && orderMetadata.RouteOrder is int order)
                    {
                        routeOrder = order;
                    }

                    builder.Metadata.Add(attribute);
                }
            }

            builder.DisplayName = routeName ?? displayName ?? builder.DisplayName;
            builder.Order = routeOrder ?? defaultOrder;

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
