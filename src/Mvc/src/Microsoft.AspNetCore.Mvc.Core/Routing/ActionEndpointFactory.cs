// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ActionEndpointFactory
    {
        private readonly RoutePatternTransformer _routePatternTransformer;

        public ActionEndpointFactory(RoutePatternTransformer routePatternTransformer)
        {
            if (routePatternTransformer == null)
            {
                throw new ArgumentNullException(nameof(routePatternTransformer));
            }

            _routePatternTransformer = routePatternTransformer;
        }

        public void AddEndpoints(
            List<Endpoint> endpoints,
            ActionDescriptor action,
            IReadOnlyList<ConventionalRouteEntry> routes,
            IReadOnlyList<Action<EndpointBuilder>> conventions)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (conventions == null)
            {
                throw new ArgumentNullException(nameof(conventions));
            }

            if (action.AttributeRouteInfo == null)
            {
                // In traditional conventional routing setup, the routes defined by a user have a static order
                // defined by how they are added into the list. We would like to maintain the same order when building
                // up the endpoints too.
                //
                // Start with an order of '1' for conventional routes as attribute routes have a default order of '0'.
                // This is for scenarios dealing with migrating existing Router based code to Endpoint Routing world.
                var conventionalRouteOrder = 1;

                // Check each of the conventional patterns to see if the action would be reachable.
                // If the action and pattern are compatible then create an endpoint with action
                // route values on the pattern.
                foreach (var route in routes)
                {
                    // A route is applicable if:
                    // 1. It has a parameter (or default value) for 'required' non-null route value
                    // 2. It does not have a parameter (or default value) for 'required' null route value
                    var updatedRoutePattern = _routePatternTransformer.SubstituteRequiredValues(route.Pattern, action.RouteValues);
                    if (updatedRoutePattern == null)
                    {
                        continue;
                    }

                    var builder = CreateEndpoint(
                        action,
                        updatedRoutePattern,
                        route.RouteName,
                        conventionalRouteOrder++,
                        route.DataTokens,
                        suppressLinkGeneration: false,
                        suppressPathMatching: false,
                        conventions);
                    endpoints.Add(builder);
                }
            }
            else
            {
                var attributeRoutePattern = RoutePatternFactory.Parse(action.AttributeRouteInfo.Template);

                // Modify the route and required values to ensure required values can be successfully subsituted.
                // Subsitituting required values into an attribute route pattern should always succeed.
                var (resolvedRoutePattern, resolvedRouteValues) = ResolveDefaultsAndRequiredValues(action, attributeRoutePattern);

                var updatedRoutePattern = _routePatternTransformer.SubstituteRequiredValues(resolvedRoutePattern, resolvedRouteValues);
                if (updatedRoutePattern == null)
                {
                    throw new InvalidOperationException("Failed to update route pattern with required values.");
                }

                var endpoint = CreateEndpoint(
                    action,
                    updatedRoutePattern,
                    action.AttributeRouteInfo.Name,
                    action.AttributeRouteInfo.Order,
                    dataTokens: null,
                    action.AttributeRouteInfo.SuppressLinkGeneration,
                    action.AttributeRouteInfo.SuppressPathMatching,
                    conventions);
                endpoints.Add(endpoint);
            }
        }

        private static (RoutePattern resolvedRoutePattern, IDictionary<string, string> resolvedRequiredValues) ResolveDefaultsAndRequiredValues(ActionDescriptor action, RoutePattern attributeRoutePattern)
        {
            RouteValueDictionary updatedDefaults = null;
            IDictionary<string, string> resolvedRequiredValues = null;

            foreach (var routeValue in action.RouteValues)
            {
                var parameter = attributeRoutePattern.GetParameter(routeValue.Key);

                if (!RouteValueEqualityComparer.Default.Equals(routeValue.Value, string.Empty))
                {
                    if (parameter == null)
                    {
                        // The attribute route has a required value with no matching parameter
                        // Add the required values without a parameter as a default
                        // e.g.
                        //   Template: "Login/{action}"
                        //   Required values: { controller = "Login", action = "Index" }
                        //   Updated defaults: { controller = "Login" }

                        if (updatedDefaults == null)
                        {
                            updatedDefaults = new RouteValueDictionary(attributeRoutePattern.Defaults);
                        }

                        updatedDefaults[routeValue.Key] = routeValue.Value;
                    }
                }
                else
                {
                    if (parameter != null)
                    {
                        // The attribute route has a null or empty required value with a matching parameter
                        // Remove the required value from the route

                        if (resolvedRequiredValues == null)
                        {
                            resolvedRequiredValues = new Dictionary<string, string>(action.RouteValues);
                        }

                        resolvedRequiredValues.Remove(parameter.Name);
                    }
                }
            }
            if (updatedDefaults != null)
            {
                attributeRoutePattern = RoutePatternFactory.Parse(action.AttributeRouteInfo.Template, updatedDefaults, parameterPolicies: null);
            }

            return (attributeRoutePattern, resolvedRequiredValues ?? action.RouteValues);
        }

        private RouteEndpoint CreateEndpoint(
            ActionDescriptor action,
            RoutePattern routePattern,
            string routeName,
            int order,
            RouteValueDictionary dataTokens,
            bool suppressLinkGeneration,
            bool suppressPathMatching,
            IReadOnlyList<Action<EndpointBuilder>> conventions)
        {

            // We don't want to close over the retrieve the Invoker Factory in ActionEndpointFactory as
            // that creates cycles in DI. Since we're creating this delegate at startup time
            // we don't want to create all of the things we use at runtime until the action
            // actually matches.
            //
            // The request delegate is already a closure here because we close over
            // the action descriptor.
            IActionInvokerFactory invokerFactory = null;

            RequestDelegate requestDelegate = (context) =>
            {
                var routeData = context.GetRouteData();
                var actionContext = new ActionContext(context, routeData, action);

                if (invokerFactory == null)
                {
                    invokerFactory = context.RequestServices.GetRequiredService<IActionInvokerFactory>();
                }

                var invoker = invokerFactory.CreateInvoker(actionContext);
                return invoker.InvokeAsync();
            };

            var builder = new RouteEndpointBuilder(requestDelegate, routePattern, order)
            {
                DisplayName = action.DisplayName,
            };

            // Add action metadata first so it has a low precedence
            if (action.EndpointMetadata != null)
            {
                foreach (var d in action.EndpointMetadata)
                {
                    builder.Metadata.Add(d);
                }
            }

            builder.Metadata.Add(action);

            if (dataTokens != null)
            {
                builder.Metadata.Add(new DataTokensMetadata(dataTokens));
            }

            builder.Metadata.Add(new RouteNameMetadata(routeName));

            // Add filter descriptors to endpoint metadata
            if (action.FilterDescriptors != null && action.FilterDescriptors.Count > 0)
            {
                foreach (var filter in action.FilterDescriptors.OrderBy(f => f, FilterDescriptorOrderComparer.Comparer).Select(f => f.Filter))
                {
                    builder.Metadata.Add(filter);
                }
            }

            if (action.ActionConstraints != null && action.ActionConstraints.Count > 0)
            {
                // We explicitly convert a few types of action constraints into MatcherPolicy+Metadata
                // to better integrate with the DFA matcher.
                //
                // Other IActionConstraint data will trigger a back-compat path that can execute
                // action constraints.
                foreach (var actionConstraint in action.ActionConstraints)
                {
                    if (actionConstraint is HttpMethodActionConstraint httpMethodActionConstraint &&
                        !builder.Metadata.OfType<HttpMethodMetadata>().Any())
                    {
                        builder.Metadata.Add(new HttpMethodMetadata(httpMethodActionConstraint.HttpMethods));
                    }
                    else if (actionConstraint is ConsumesAttribute consumesAttribute &&
                        !builder.Metadata.OfType<ConsumesMetadata>().Any())
                    {
                        builder.Metadata.Add(new ConsumesMetadata(consumesAttribute.ContentTypes.ToArray()));
                    }
                    else if (!builder.Metadata.Contains(actionConstraint))
                    {
                        // The constraint might have been added earlier, e.g. it is also a filter descriptor
                        builder.Metadata.Add(actionConstraint);
                    }
                }
            }

            if (suppressLinkGeneration)
            {
                builder.Metadata.Add(new SuppressLinkGenerationMetadata());
            }

            if (suppressPathMatching)
            {
                builder.Metadata.Add(new SuppressMatchingMetadata());
            }

            for (var i = 0; i < conventions.Count; i++)
            {
                conventions[i](builder);
            }

            return (RouteEndpoint)builder.Build();
        }
    }
}
