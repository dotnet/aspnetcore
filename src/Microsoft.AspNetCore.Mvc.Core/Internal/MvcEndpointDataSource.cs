// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    internal class MvcEndpointDataSource : EndpointDataSource
    {
        private readonly IActionDescriptorCollectionProvider _actions;
        private readonly MvcEndpointInvokerFactory _invokerFactory;
        private readonly IActionDescriptorChangeProvider[] _actionDescriptorChangeProviders;
        private readonly List<Endpoint> _endpoints;

        private IChangeToken _changeToken;

        public MvcEndpointDataSource(
            IActionDescriptorCollectionProvider actions,
            MvcEndpointInvokerFactory invokerFactory,
            IEnumerable<IActionDescriptorChangeProvider> actionDescriptorChangeProviders)
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

            _actions = actions;
            _invokerFactory = invokerFactory;
            _actionDescriptorChangeProviders = actionDescriptorChangeProviders.ToArray();

            _endpoints = new List<Endpoint>();

            InitializeEndpoints();
        }

        private void InitializeEndpoints()
        {
            // note: this code has haxxx. This will only work in some constrained scenarios
            foreach (var action in _actions.ActionDescriptors.Items)
            {
                if (action.AttributeRouteInfo == null)
                {
                    // Action does not have an attribute route
                    continue;
                }

                RequestDelegate invokerDelegate = (context) =>
                {
                    var values = context.Features.Get<IEndpointFeature>().Values;
                    var routeData = new RouteData();
                    foreach (var kvp in values)
                    {
                        routeData.Values.Add(kvp.Key, kvp.Value);
                    }

                    var actionContext = new ActionContext(context, routeData, action);

                    var invoker = _invokerFactory.CreateInvoker(actionContext);
                    return invoker.InvokeAsync();
                };

                var metadata = new List<object>();

                // Add filter descriptors to endpoint metadata
                metadata.AddRange(action.FilterDescriptors.OrderBy(f => f, FilterDescriptorOrderComparer.Comparer).Select(f => f.Filter));

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

                _endpoints.Add(new MatcherEndpoint(
                    next => invokerDelegate,
                    action.AttributeRouteInfo.Template,
                    action.RouteValues,
                    action.AttributeRouteInfo.Order,
                    metadataCollection,
                    action.DisplayName));
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

        public override IReadOnlyList<Endpoint> Endpoints => _endpoints;
    }
}