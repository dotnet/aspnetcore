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
            return WrapConventionBuilder(
                endpoints.MapGet(pattern, MapActionExpressionTreeBuilder.BuildRequestDelegate(action)),
                pattern,
                action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP POST requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapPost(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            return WrapConventionBuilder(
                endpoints.MapPost(pattern, MapActionExpressionTreeBuilder.BuildRequestDelegate(action)),
                pattern,
                action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP PUT requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that canaction be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapPut(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            return WrapConventionBuilder(
                endpoints.MapPut(pattern, MapActionExpressionTreeBuilder.BuildRequestDelegate(action)),
                pattern,
                action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP DELETE requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapDelete(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            return WrapConventionBuilder(
                endpoints.MapDelete(pattern, MapActionExpressionTreeBuilder.BuildRequestDelegate(action)),
                pattern,
                action);
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
        public static IEndpointConventionBuilder MapMethods(
           this IEndpointRouteBuilder endpoints,
           string pattern,
           IEnumerable<string> httpMethods,
           Delegate action)
        {
            return WrapConventionBuilder(
                endpoints.MapMethods(pattern, httpMethods, MapActionExpressionTreeBuilder.BuildRequestDelegate(action)),
                pattern,
                action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder Map(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            Delegate action)
        {
            return WrapConventionBuilder(
                endpoints.Map(pattern, MapActionExpressionTreeBuilder.BuildRequestDelegate(action)),
                pattern,
                action);
        }

        /// <summary>
        /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
        /// for the specified pattern.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="action">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder Map(
            this IEndpointRouteBuilder endpoints,
            RoutePattern pattern,
            Delegate action)
        {
            return WrapConventionBuilder(
                endpoints.Map(pattern, MapActionExpressionTreeBuilder.BuildRequestDelegate(action)),
                pattern.RawText,
                action);
        }

        private static MapActionEndpointConventionBuilder WrapConventionBuilder(
            IEndpointConventionBuilder endpointConventionBuilder,
            string? pattern,
            Delegate action)
        {
            var attributes = action.Method.GetCustomAttributes();
            string? routeName = null;
            int? routeOrder = null;

            foreach (var attribute in attributes)
            {
                if (attribute is IRoutePatternMetadata patternMetadata && patternMetadata.RoutePattern is { })
                {
                    throw new InvalidOperationException($"'{attribute.GetType()}' implements {nameof(IRoutePatternMetadata)} which is not supported my this method.");
                }
                if (attribute is IHttpMethodMetadata methodMetadata && methodMetadata.HttpMethods.Any())
                {
                    throw new InvalidOperationException($"'{attribute.GetType()}' implements {nameof(IHttpMethodMetadata)} which is not supported my this method.");
                }

                if (attribute is IRouteNameMetadata nameMetadata && nameMetadata.RouteName is string name)
                {
                    routeName = name;
                }
                if (attribute is IRouteOrderMetadata orderMetadata && orderMetadata.RouteOrder is int order)
                {
                    routeOrder = order;
                }
            }

            const int defaultOrder = 0;

            endpointConventionBuilder.Add(endpointBuilder =>
            {
                foreach (var attribute in attributes)
                {
                    endpointBuilder.Metadata.Add(attribute);
                }

                endpointBuilder.DisplayName = routeName ?? pattern;

                ((RouteEndpointBuilder)endpointBuilder).Order = routeOrder ?? defaultOrder;
            });

            return new MapActionEndpointConventionBuilder(endpointConventionBuilder);
        }

    }
}
