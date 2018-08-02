// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class TreeRouterMatcherBuilder : MatcherBuilder
    {
        private readonly List<MatcherEndpoint> _endpoints;

        public TreeRouterMatcherBuilder()
        {
            _endpoints = new List<MatcherEndpoint>();
        }

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public override Matcher Build()
        {
            var builder = new TreeRouteBuilder(
                NullLoggerFactory.Instance,
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                new DefaultInlineConstraintResolver(Options.Create(new RouteOptions())));

            var selector = new DefaultEndpointSelector(Array.Empty<MatcherPolicy>());

            var groups = _endpoints
                .GroupBy(e => (e.Order, e.RoutePattern.InboundPrecedence, e.RoutePattern.RawText))
                .OrderBy(g => g.Key.Order)
                .ThenBy(g => g.Key.InboundPrecedence);

            var routes = new RouteCollection();

            foreach (var group in groups)
            {
                var candidates = group.ToArray();

                // MatcherEndpoint.Values contains the default values parsed from the template
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
            private readonly MatcherEndpoint[] _candidates;
            private readonly int[] _scores;

            public SelectorRouter(EndpointSelector selector, MatcherEndpoint[] candidates)
            {
                _selector = selector;
                _candidates = candidates;

                _scores = new int[_candidates.Length];
            }

            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                throw new NotImplementedException();
            }

            public async Task RouteAsync(RouteContext context)
            {
                var feature = context.HttpContext.Features.Get<IEndpointFeature>();

                // This is needed due to a quirk of our tests - they reuse the endpoint feature.
                feature.Endpoint = null;
                
                await _selector.SelectAsync(context.HttpContext, feature, new CandidateSet(_candidates, _scores));
                if (feature.Endpoint != null)
                {
                    context.Handler = (_) => Task.CompletedTask;
                }
            }
        }
    }
}
