// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class MvcEndpointDataSource : EndpointDataSource
    {
        private readonly IActionDescriptorCollectionProvider _actions;
        private readonly ActionEndpointFactory _builderFactory;

        // The following are protected by this lock for WRITES only. This pattern is similar
        // to DefaultActionDescriptorChangeProvider - see comments there for details on
        // all of the threading behaviors.
        private readonly object _lock = new object();
        private List<Endpoint> _endpoints;
        private CancellationTokenSource _cancellationTokenSource;
        private IChangeToken _changeToken;

        public MvcEndpointDataSource(
            IActionDescriptorCollectionProvider actions,
            ActionEndpointFactory builderFactory)
        {
            _actions = actions;
            _builderFactory = builderFactory;

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
                        _builderFactory.AddConventionalRoutedEndpoints(endpoints, action, ConventionalEndpointInfos);
                    }
                    else
                    {
                        var convention = ResolveActionConventionBuilder(action);
                        if (convention == null)
                        {
                            // No convention builder for this action
                            // Do not create an endpoint for it
                            continue;
                        }

                        _builderFactory.AddAttributeRoutedEndpoint(endpoints, action, convention.Conventions);
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
    }
}
