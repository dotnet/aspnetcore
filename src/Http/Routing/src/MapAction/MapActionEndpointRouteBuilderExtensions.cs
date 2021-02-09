// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.MapAction;

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
        /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
        public static IEndpointConventionBuilder MapAction(
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

            var routeAttributes = action.Method.GetCustomAttributes().OfType<IRouteTemplateProvider>();
            var conventionBuilders = new List<IEndpointConventionBuilder>();

            const int defaultOrder = 0;

            foreach (var routeAttribute in routeAttributes)
            {
                if (routeAttribute.Template is null)
                {
                    continue;
                }

                var conventionBuilder = endpoints.Map(routeAttribute.Template, requestDelegate);

                conventionBuilder.Add(endpointBuilder =>
                {
                    foreach (var attribute in action.Method.GetCustomAttributes())
                    {
                        endpointBuilder.Metadata.Add(attribute);
                    }

                    endpointBuilder.DisplayName = routeAttribute.Name ?? routeAttribute.Template;

                    ((RouteEndpointBuilder)endpointBuilder).Order = routeAttribute.Order ?? defaultOrder;
                });

                conventionBuilders.Add(conventionBuilder);
            }

            if (conventionBuilders.Count == 0)
            {
                throw new InvalidOperationException("Action must have a pattern. Is it missing a Route attribute?");
            }

            return new CompositeEndpointConventionBuilder(conventionBuilders);
        }

        private class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
        {
            private readonly List<IEndpointConventionBuilder> _endpointConventionBuilders;

            public CompositeEndpointConventionBuilder(List<IEndpointConventionBuilder> endpointConventionBuilders)
            {
                _endpointConventionBuilders = endpointConventionBuilders;
            }

            public void Add(Action<EndpointBuilder> convention)
            {
                foreach (var endpointConventionBuilder in _endpointConventionBuilders)
                {
                    endpointConventionBuilder.Add(convention);
                }
            }
        }
    }
}
