// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class MvcEndpointDataSource : EndpointDataSource
    {
        private readonly IActionDescriptorCollectionProvider _actions;
        private readonly MvcEndpointInvokerFactory _invokerFactory;
        private readonly ParameterPolicyFactory _parameterPolicyFactory;
        private readonly RoutePatternTransformer _routePatternTransformer;

        // The following are protected by this lock for WRITES only. This pattern is similar
        // to DefaultActionDescriptorChangeProvider - see comments there for details on
        // all of the threading behaviors.
        private readonly object _lock = new object();
        private List<Endpoint> _endpoints;
        private CancellationTokenSource _cancellationTokenSource;
        private IChangeToken _changeToken;

        public MvcEndpointDataSource(
            IActionDescriptorCollectionProvider actions,
            MvcEndpointInvokerFactory invokerFactory,
            ParameterPolicyFactory parameterPolicyFactory,
            RoutePatternTransformer routePatternTransformer)
        {
            _actions = actions;
            _invokerFactory = invokerFactory;
            _parameterPolicyFactory = parameterPolicyFactory;
            _routePatternTransformer = routePatternTransformer;

            ConventionalEndpointInfos = new List<MvcEndpointInfo>();
            AttributeRoutingConventionResolvers = new List<Func<ActionDescriptor, DefaultEndpointConventionBuilder>>();

            // IMPORTANT: this needs to be the last thing we do in the constructor. Change notifications can happen immediately!
            //
            // It's possible for someone to override the collection provider without providing
            // change notifications. If that's the case we won't process changes.
            if (actions is ActionDescriptorCollectionProvider collectionProviderWithChangeToken)
            {
                ChangeToken.OnChange(
                    () => collectionProviderWithChangeToken.GetChangeToken(),
                    UpdateEndpoints);
            }
        }

        public List<MvcEndpointInfo> ConventionalEndpointInfos { get; }

        public List<Func<ActionDescriptor, DefaultEndpointConventionBuilder>> AttributeRoutingConventionResolvers { get; }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                Initialize();
                Debug.Assert(_changeToken != null);
                Debug.Assert(_endpoints != null);
                return _endpoints;
            }
        }

        public override IChangeToken GetChangeToken()
        {
            Initialize();
            Debug.Assert(_changeToken != null);
            Debug.Assert(_endpoints != null);
            return _changeToken;
        }

        private void Initialize()
        {
            if (_endpoints == null)
            {
                lock (_lock)
                {
                    if (_endpoints == null)
                    {
                        UpdateEndpoints();
                    }
                }
            }
        }

        private void UpdateEndpoints()
        {
            lock (_lock)
            {
                var endpoints = new List<Endpoint>();

                foreach (var action in _actions.ActionDescriptors.Items)
                {
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
                        foreach (var endpointInfo in ConventionalEndpointInfos)
                        {
                            // An 'endpointInfo' is applicable if:
                            // 1. It has a parameter (or default value) for 'required' non-null route value
                            // 2. It does not have a parameter (or default value) for 'required' null route value
                            var updatedRoutePattern = _routePatternTransformer.SubstituteRequiredValues(endpointInfo.ParsedPattern, action.RouteValues);

                            if (updatedRoutePattern == null)
                            {
                                continue;
                            }

                            var endpoint = CreateEndpoint(
                                action,
                                updatedRoutePattern,
                                endpointInfo.Name,
                                conventionalRouteOrder++,
                                endpointInfo.DataTokens,
                                false,
                                false,
                                endpointInfo.Conventions);
                            endpoints.Add(endpoint);
                        }
                    }
                    else
                    {
                        var conventionBuilder = ResolveActionConventionBuilder(action);
                        if (conventionBuilder == null)
                        {
                            // No convention builder for this action
                            // Do not create an endpoint for it
                            continue;
                        }

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
                            conventionBuilder.Conventions);
                        endpoints.Add(endpoint);
                    }
                }

                // See comments in DefaultActionDescriptorCollectionProvider. These steps are done
                // in a specific order to ensure callers always see a consistent state.

                // Step 1 - capture old token
                var oldCancellationTokenSource = _cancellationTokenSource;

                // Step 2 - update endpoints
                _endpoints = endpoints;

                // Step 3 - create new change token
                _cancellationTokenSource = new CancellationTokenSource();
                _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);

                // Step 4 - trigger old token
                oldCancellationTokenSource?.Cancel();
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

        private DefaultEndpointConventionBuilder ResolveActionConventionBuilder(ActionDescriptor action)
        {
            foreach (var filter in AttributeRoutingConventionResolvers)
            {
                var conventionBuilder = filter(action);
                if (conventionBuilder != null)
                {
                    return conventionBuilder;
                }
            }

            return null;
        }

        private RouteEndpoint CreateEndpoint(
            ActionDescriptor action,
            RoutePattern routePattern,
            string routeName,
            int order,
            RouteValueDictionary dataTokens,
            bool suppressLinkGeneration,
            bool suppressPathMatching,
            List<Action<EndpointBuilder>> conventions)
        {
            RequestDelegate requestDelegate = (context) =>
            {
                var routeData = context.GetRouteData();

                var actionContext = new ActionContext(context, routeData, action);

                var invoker = _invokerFactory.CreateInvoker(actionContext);
                return invoker.InvokeAsync();
            };

            var endpointBuilder = new RouteEndpointBuilder(requestDelegate, routePattern, order);
            AddEndpointMetadata(
                endpointBuilder.Metadata,
                action,
                routeName,
                dataTokens,
                suppressLinkGeneration,
                suppressPathMatching);

            endpointBuilder.DisplayName = action.DisplayName;

            // REVIEW: When should conventions be run
            // Metadata should have lower precedence that data source metadata
            if (conventions != null)
            {
                foreach (var convention in conventions)
                {
                    convention(endpointBuilder);
                }
            }

            return (RouteEndpoint)endpointBuilder.Build();
        }

        private static void AddEndpointMetadata(
            IList<object> metadata,
            ActionDescriptor action,
            string routeName,
            RouteValueDictionary dataTokens,
            bool suppressLinkGeneration,
            bool suppressPathMatching)
        {
            // Add action metadata first so it has a low precedence
            if (action.EndpointMetadata != null)
            {
                foreach (var d in action.EndpointMetadata)
                {
                    metadata.Add(d);
                }
            }

            metadata.Add(action);

            if (dataTokens != null)
            {
                metadata.Add(new DataTokensMetadata(dataTokens));
            }

            metadata.Add(new RouteNameMetadata(routeName));

            // Add filter descriptors to endpoint metadata
            if (action.FilterDescriptors != null && action.FilterDescriptors.Count > 0)
            {
                foreach (var filter in action.FilterDescriptors.OrderBy(f => f, FilterDescriptorOrderComparer.Comparer).Select(f => f.Filter))
                {
                    metadata.Add(filter);
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
                        !metadata.OfType<HttpMethodMetadata>().Any())
                    {
                        metadata.Add(new HttpMethodMetadata(httpMethodActionConstraint.HttpMethods));
                    }
                    else if (actionConstraint is ConsumesAttribute consumesAttribute &&
                        !metadata.OfType<ConsumesMetadata>().Any())
                    {
                        metadata.Add(new ConsumesMetadata(consumesAttribute.ContentTypes.ToArray()));
                    }
                    else if (!metadata.Contains(actionConstraint))
                    {
                        // The constraint might have been added earlier, e.g. it is also a filter descriptor
                        metadata.Add(actionConstraint);
                    }
                }
            }

            if (suppressLinkGeneration)
            {
                metadata.Add(new SuppressLinkGenerationMetadata());
            }

            if (suppressPathMatching)
            {
                metadata.Add(new SuppressMatchingMetadata());
            }
        }
    }
}
