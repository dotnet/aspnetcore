// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    internal class MvcEndpointDataSource : EndpointDataSource
    {
        private readonly IActionDescriptorCollectionProvider _actions;
        private readonly MvcEndpointInvokerFactory _invokerFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IActionDescriptorChangeProvider[] _actionDescriptorChangeProviders;
        private readonly List<Endpoint> _endpoints;
        private readonly object _lock = new object();

        private IChangeToken _changeToken;
        private bool _initialized;

        public MvcEndpointDataSource(
            IActionDescriptorCollectionProvider actions,
            MvcEndpointInvokerFactory invokerFactory,
            IEnumerable<IActionDescriptorChangeProvider> actionDescriptorChangeProviders,
            IServiceProvider serviceProvider)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            if (invokerFactory == null)
            {
                throw new ArgumentNullException(nameof(invokerFactory));
            }

            if (actionDescriptorChangeProviders == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptorChangeProviders));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _actions = actions;
            _invokerFactory = invokerFactory;
            _serviceProvider = serviceProvider;
            _actionDescriptorChangeProviders = actionDescriptorChangeProviders.ToArray();

            _endpoints = new List<Endpoint>();
            ConventionalEndpointInfos = new List<MvcEndpointInfo>();
        }

        private void InitializeEndpoints()
        {
            foreach (var action in _actions.ActionDescriptors.Items)
            {
                if (action.AttributeRouteInfo == null)
                {
                    // In traditional conventional routing setup, the routes defined by a user have a static order
                    // defined by how they are added into the list. We would like to maintain the same order when building
                    // up the endpoints too.
                    //
                    // Start with an order of '1' for conventional routes as attribute routes have a default order of '0'.
                    // This is for scenarios dealing with migrating existing Routing based code to Dispatcher world.
                    var conventionalRouteOrder = 0;

                    // Check each of the conventional templates to see if the action would be reachable
                    // If the action and template are compatible then create an endpoint with the
                    // area/controller/action parameter parts replaced with literals
                    //
                    // e.g. {controller}/{action} with HomeController.Index and HomeController.Login
                    // would result in endpoints:
                    // - Home/Index
                    // - Home/Login
                    foreach (var endpointInfo in ConventionalEndpointInfos)
                    {
                        var actionRouteValues = action.RouteValues;
                        var endpointTemplateSegments = endpointInfo.ParsedTemplate.Segments;

                        if (MatchRouteValue(action, endpointInfo, "Area")
                            && MatchRouteValue(action, endpointInfo, "Controller")
                            && MatchRouteValue(action, endpointInfo, "Action"))
                        {
                            var newEndpointTemplate = TemplateParser.Parse(endpointInfo.Template);

                            for (var i = 0; i < newEndpointTemplate.Segments.Count; i++)
                            {
                                // Check if the template can be shortened because the remaining parameters are optional
                                //
                                // e.g. Matching template {controller=Home}/{action=Index}/{id?} against HomeController.Index
                                // can resolve to the following endpoints:
                                // - /Home/Index/{id?}
                                // - /Home
                                // - /
                                if (UseDefaultValuePlusRemainingSegementsOptional(i, action, endpointInfo, newEndpointTemplate))
                                {
                                    var subTemplate = RouteTemplateWriter.ToString(newEndpointTemplate.Segments.Take(i));

                                    var subEndpoint = CreateEndpoint(
                                        action,
                                        endpointInfo.Name,
                                        subTemplate,
                                        endpointInfo.Defaults,
                                        ++conventionalRouteOrder,
                                        endpointInfo);
                                    _endpoints.Add(subEndpoint);
                                }

                                var segment = newEndpointTemplate.Segments[i];
                                for (var j = 0; j < segment.Parts.Count; j++)
                                {
                                    var part = segment.Parts[j];

                                    if (part.IsParameter && IsMvcParameter(part.Name))
                                    {
                                        // Replace parameter with literal value
                                        segment.Parts[j] = TemplatePart.CreateLiteral(action.RouteValues[part.Name]);
                                    }
                                }
                            }

                            var newTemplate = RouteTemplateWriter.ToString(newEndpointTemplate.Segments);

                            var endpoint = CreateEndpoint(
                                action,
                                endpointInfo.Name,
                                newTemplate,
                                endpointInfo.Defaults,
                                ++conventionalRouteOrder,
                                endpointInfo);
                            _endpoints.Add(endpoint);
                        }
                    }
                }
                else
                {
                    var endpoint = CreateEndpoint(
                        action,
                        action.AttributeRouteInfo.Name,
                        action.AttributeRouteInfo.Template,
                        nonInlineDefaults: null,
                        action.AttributeRouteInfo.Order,
                        action.AttributeRouteInfo);
                    _endpoints.Add(endpoint);
                }
            }
        }

        private bool IsMvcParameter(string name)
        {
            if (string.Equals(name, "Area", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Controller", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Action", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private bool UseDefaultValuePlusRemainingSegementsOptional(
            int segmentIndex,
            ActionDescriptor action,
            MvcEndpointInfo endpointInfo,
            RouteTemplate template)
        {
            // Check whether the remaining segments are all optional and one or more of them is
            // for area/controller/action and has a default value
            var usedDefaultValue = false;

            for (var i = segmentIndex; i < template.Segments.Count; i++)
            {
                var segment = template.Segments[i];
                for (var j = 0; j < segment.Parts.Count; j++)
                {
                    var part = segment.Parts[j];
                    if (part.IsOptional || part.IsOptionalSeperator || part.IsCatchAll)
                    {
                        continue;
                    }
                    if (part.IsParameter)
                    {
                        if (IsMvcParameter(part.Name))
                        {
                            if (endpointInfo.MergedDefaults[part.Name] is string defaultValue
                                && action.RouteValues.TryGetValue(part.Name, out var routeValue)
                                && string.Equals(defaultValue, routeValue, StringComparison.OrdinalIgnoreCase))
                            {
                                usedDefaultValue = true;
                                continue;
                            }
                        }
                    }

                    // Stop because there is a non-optional/non-defaulted trailing value
                    return false;
                }
            }

            return usedDefaultValue;
        }

        private bool MatchRouteValue(ActionDescriptor action, MvcEndpointInfo endpointInfo, string routeKey)
        {
            if (!action.RouteValues.TryGetValue(routeKey, out var actionValue) || string.IsNullOrWhiteSpace(actionValue))
            {
                // Action does not have a value for this routeKey, most likely because action is not in an area
                // Check that the template does not have a parameter for the routeKey
                var matchingParameter = endpointInfo.ParsedTemplate.Parameters.SingleOrDefault(p => string.Equals(p.Name, routeKey, StringComparison.OrdinalIgnoreCase));
                if (matchingParameter == null)
                {
                    return true;
                }
            }
            else
            {
                if (endpointInfo.MergedDefaults != null && string.Equals(actionValue, endpointInfo.MergedDefaults[routeKey] as string, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var matchingParameter = endpointInfo.ParsedTemplate.Parameters.SingleOrDefault(p => string.Equals(p.Name, routeKey, StringComparison.OrdinalIgnoreCase));
                if (matchingParameter != null)
                {
                    // Check that the value matches against constraints on that parameter
                    // e.g. For {controller:regex((Home|Login))} the controller value must match the regex
                    //
                    // REVIEW: This is really ugly
                    if (endpointInfo.Constraints.TryGetValue(routeKey, out var constraint)
                        && !constraint.Match(new DefaultHttpContext() { RequestServices = _serviceProvider }, new DummyRouter(), routeKey, new RouteValueDictionary(action.RouteValues), RouteDirection.IncomingRequest))
                    {
                        // Did not match constraint
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        private class DummyRouter : IRouter
        {
            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                return null;
            }

            public Task RouteAsync(RouteContext context)
            {
                return Task.CompletedTask;
            }
        }

        private MatcherEndpoint CreateEndpoint(
            ActionDescriptor action,
            string routeName,
            string template,
            object nonInlineDefaults,
            int order,
            object source)
        {
            RequestDelegate invokerDelegate = (context) =>
            {
                var values = context.Features.Get<IEndpointFeature>().Values;
                var routeData = new RouteData();
                foreach (var kvp in values)
                {
                    if (kvp.Value != null)
                    {
                        routeData.Values.Add(kvp.Key, kvp.Value);
                    }
                }

                var actionContext = new ActionContext(context, routeData, action);

                var invoker = _invokerFactory.CreateInvoker(actionContext);
                return invoker.InvokeAsync();
            };

            var metadata = new List<object>();
            // REVIEW: Used for debugging. Consider removing before release
            metadata.Add(source);
            metadata.Add(action);

            if (!string.IsNullOrEmpty(routeName))
            {
                metadata.Add(new RouteNameMetadata(routeName));
            }

            // Add filter descriptors to endpoint metadata
            if (action.FilterDescriptors != null && action.FilterDescriptors.Count > 0)
            {
                metadata.AddRange(action.FilterDescriptors.OrderBy(f => f, FilterDescriptorOrderComparer.Comparer)
                    .Select(f => f.Filter));
            }

            if (action.ActionConstraints != null && action.ActionConstraints.Count > 0)
            {
                foreach (var actionConstraint in action.ActionConstraints)
                {
                    if (actionConstraint is HttpMethodActionConstraint httpMethodActionConstraint)
                    {
                        metadata.Add(new HttpMethodEndpointConstraint(httpMethodActionConstraint.HttpMethods));
                    }
                }
            }

            var metadataCollection = new EndpointMetadataCollection(metadata);
            var endpoint = new MatcherEndpoint(
                next => invokerDelegate,
                template,
                new RouteValueDictionary(nonInlineDefaults),
                new RouteValueDictionary(action.RouteValues),
                order,
                metadataCollection,
                action.DisplayName);

            // Use defaults after the endpoint is created as it merges both the inline and
            // non-inline defaults into one.
            EnsureRequiredValuesInDefaults(endpoint.RequiredValues, endpoint.Defaults);

            return endpoint;
        }

        // Ensure required values are a subset of defaults
        // Examples:
        //
        // Template: {controller}/{action}/{category}/{id?}
        // Defaults(in-line or non in-line): category=products
        // Required values: controller=foo, action=bar
        // Final constructed template: foo/bar/{category}/{id?}
        // Final defaults: controller=foo, action=bar, category=products
        //
        // Template: {controller=Home}/{action=Index}/{category=products}/{id?}
        // Defaults: controller=Home, action=Index, category=products
        // Required values: controller=foo, action=bar
        // Final constructed template: foo/bar/{category}/{id?}
        // Final defaults: controller=foo, action=bar, category=products
        private void EnsureRequiredValuesInDefaults(RouteValueDictionary requiredValues, RouteValueDictionary defaults)
        {
            foreach (var kvp in requiredValues)
            {
                defaults[kvp.Key] = kvp.Value;
            }
        }

        private IChangeToken GetCompositeChangeToken()
        {
            if (_actionDescriptorChangeProviders.Length == 1)
            {
                return _actionDescriptorChangeProviders[0].GetChangeToken();
            }

            var changeTokens = new IChangeToken[_actionDescriptorChangeProviders.Length];
            for (var i = 0; i < _actionDescriptorChangeProviders.Length; i++)
            {
                changeTokens[i] = _actionDescriptorChangeProviders[i].GetChangeToken();
            }

            return new CompositeChangeToken(changeTokens);
        }

        public override IChangeToken ChangeToken
        {
            get
            {
                if (_changeToken == null)
                {
                    _changeToken = GetCompositeChangeToken();
                }

                return _changeToken;
            }
        }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                if (!_initialized)
                {
                    lock (_lock)
                    {
                        if (!_initialized)
                        {
                            InitializeEndpoints();
                            _initialized = true;
                        }
                    }
                }

                return _endpoints;
            }
        }

        // REVIEW: Infos added after endpoints are initialized will not be used
        public List<MvcEndpointInfo> ConventionalEndpointInfos { get; }

        private class RouteNameMetadata : IRouteNameMetadata
        {
            public RouteNameMetadata(string routeName)
            {
                Name = routeName;
            }

            public string Name { get; }
        }
    }
}