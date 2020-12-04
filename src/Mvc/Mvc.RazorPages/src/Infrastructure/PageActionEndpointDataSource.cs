// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class PageActionEndpointDataSource : ActionEndpointDataSourceBase
    {
        private readonly ActionEndpointFactory _endpointFactory;
        private readonly OrderedEndpointsSequenceProvider _orderSequence;
        private readonly DefaultPageLoader _pageLoader;

        public PageActionEndpointDataSource(
            PageActionEndpointDataSourceIdProvider dataSourceIdProvider,
            IActionDescriptorCollectionProvider actions,
            ActionEndpointFactory endpointFactory,
            PageLoader pageLoader,
            OrderedEndpointsSequenceProvider orderedEndpoints)
            : base(actions)
        {
            DataSourceId = dataSourceIdProvider.CreateId();
            _endpointFactory = endpointFactory;
            _orderSequence = orderedEndpoints;
            DefaultBuilder = new PageActionEndpointConventionBuilder(Lock, Conventions);

            // If we haven't replaced the default view compiler then we can load the page directly
            // synchronously and in memory
            if (pageLoader is DefaultPageLoader pl && pl.Compiler is DefaultViewCompiler)
            {
                _pageLoader = pl;
            }

            // IMPORTANT: this needs to be the last thing we do in the constructor.
            // Change notifications can happen immediately!
            Subscribe();
        }

        public int DataSourceId { get; }

        public PageActionEndpointConventionBuilder DefaultBuilder { get; }

        // Used to control whether we create 'inert' (non-routable) endpoints for use in dynamic
        // selection. Set to true by builder methods that do dynamic/fallback selection.
        public bool CreateInertEndpoints { get; set; }

        protected override List<Endpoint> CreateEndpoints(IReadOnlyList<ActionDescriptor> actions, IReadOnlyList<Action<EndpointBuilder>> conventions)
        {
            var endpoints = new List<Endpoint>();
            var routeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i] is PageActionDescriptor action)
                {
                    if (_pageLoader != null)
                    {
                        // The default view compiler does a dictionary lookup so we can rely on that always completing synchronously
                        // this lets us avoid lots of per-request work as the compiled page is already available at
                        // startup for this page so we can base the endpoint on that directly.
                        var compiledTask = _pageLoader.LoadWithoutEndpoint(action);

                        // This should always complete synchronously
                        Debug.Assert(compiledTask.IsCompleted);

                        // Add the compiled descriptor directly
                        var compiledActionDescriptor = compiledTask.GetAwaiter().GetResult();
                        _endpointFactory.AddEndpoints(endpoints, routeNames, compiledActionDescriptor, Array.Empty<ConventionalRouteEntry>(), conventions, CreateInertEndpoints);
                    }
                    else
                    {
                        _endpointFactory.AddEndpoints(endpoints, routeNames, action, Array.Empty<ConventionalRouteEntry>(), conventions, CreateInertEndpoints);
                    }
                }
            }

            return endpoints;
        }

        internal void AddDynamicPageEndpoint(IEndpointRouteBuilder endpoints, string pattern, Type transformerType, object state, int? order = null)
        {
            CreateInertEndpoints = true;
            lock (Lock)
            {
                order ??= _orderSequence.GetNext();

                endpoints.Map(
                    pattern,
                    context =>
                    {
                        throw new InvalidOperationException("This endpoint is not expected to be executed directly.");
                    })
                    .Add(b =>
                    {
                        ((RouteEndpointBuilder)b).Order = order.Value;
                        b.Metadata.Add(new DynamicPageRouteValueTransformerMetadata(transformerType, state));
                        b.Metadata.Add(new PageEndpointDataSourceIdMetadata(DataSourceId));
                    });
            }
        }
    }
}
