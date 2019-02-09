// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class MvcEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
    {
        private readonly IActionDescriptorCollectionProvider _actions;
        private readonly ActionEndpointFactory _endpointFactory;

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
            _endpointFactory = builderFactory;

            Conventions = new List<Action<EndpointBuilder>>();
            Routes = new List<ConventionalRouteEntry>();

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

        public List<Action<EndpointBuilder>> Conventions { get; }

        public List<ConventionalRouteEntry> Routes { get; }

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

        public void Add(Action<EndpointBuilder> convention)
        {
            if (convention == null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            lock (_lock)
            {
                Conventions.Add(convention);
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
                    _endpointFactory.AddEndpoints(endpoints, action, Routes, Conventions);
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
    }
}
