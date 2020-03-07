// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class TreeRouterMatcherBuilder : MatcherBuilder
    {
        private readonly List<RouteEndpoint> _endpoints;

        public TreeRouterMatcherBuilder()
        {
            _endpoints = new List<RouteEndpoint>();
        }

        public override void AddEndpoint(RouteEndpoint endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public override Matcher Build()
        {
            var builder = new TreeRouteBuilder(
                NullLoggerFactory.Instance,
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                new DefaultInlineConstraintResolver(Options.Create(new RouteOptions()), new TestServiceProvider()));

            var selector = new DefaultEndpointSelector();

            var groups = _endpoints
                .GroupBy(e => (e.Order, e.RoutePattern.InboundPrecedence, e.RoutePattern.RawText))
                .OrderBy(g => g.Key.Order)
                .ThenBy(g => g.Key.InboundPrecedence);

            var routes = new RouteCollection();

            foreach (var group in groups)
            {
                var candidates = group.ToArray();

                // RouteEndpoint.Values contains the default values parsed from the template
                // as well as those specified with a literal. We need to separate those
                // for legacy cases.
                var endpoint = group.First();
                var defaults = new RouteValueDictionary(endpoint.RoutePattern.Defaults);
                for (var i = 0; i < endpoint.RoutePattern.Parameters.Count; i++)
                {
                    var parameter = endpoint.RoutePattern.Parameters[i];
                    if (parameter.Default != null)
                    {
                        defaults.Remove(parameter.Name);
                    }
                }

                builder.MapInbound(
                    new SelectorRouter(selector, candidates),
                    new RouteTemplate(endpoint.RoutePattern),
                    routeName: null,
                    order: endpoint.Order);
            }

            return new TreeRouterMatcher(builder.Build());
        }

        private class SelectorRouter : IRouter
        {
            private readonly EndpointSelector _selector;
            private readonly RouteEndpoint[] _candidates;
            private readonly RouteValueDictionary[] _values;
            private readonly int[] _scores;

            public SelectorRouter(EndpointSelector selector, RouteEndpoint[] candidates)
            {
                _selector = selector;
                _candidates = candidates;

                _values = new RouteValueDictionary[_candidates.Length];
                _scores = new int[_candidates.Length];
            }

            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                throw new NotImplementedException();
            }

            public async Task RouteAsync(RouteContext routeContext)
            {
                // This is needed due to a quirk of our tests - they reuse the endpoint feature.
                routeContext.HttpContext.SetEndpoint(null);

                await _selector.SelectAsync(routeContext.HttpContext, new CandidateSet(_candidates, _values, _scores));
                if (routeContext.HttpContext.GetEndpoint() != null)
                {
                    routeContext.Handler = (_) => Task.CompletedTask;
                }
            }
        }
    }
}
