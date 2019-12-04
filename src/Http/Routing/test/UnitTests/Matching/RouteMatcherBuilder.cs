// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class RouteMatcherBuilder : MatcherBuilder
    {
        private readonly IInlineConstraintResolver _constraintResolver;
        private readonly List<RouteEndpoint> _endpoints;

        public RouteMatcherBuilder()
        {
            _constraintResolver = new DefaultInlineConstraintResolver(Options.Create(new RouteOptions()), new TestServiceProvider());
            _endpoints = new List<RouteEndpoint>();
        }

        public override void AddEndpoint(RouteEndpoint endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public override Matcher Build()
        {
            var selector = new DefaultEndpointSelector();

            var groups = _endpoints
                .GroupBy(e => (e.Order, e.RoutePattern.InboundPrecedence, e.RoutePattern.RawText))
                .OrderBy(g => g.Key.Order)
                .ThenBy(g => g.Key.InboundPrecedence);

            var routes = new RouteCollection();

            foreach (var group in groups)
            {
                var candidates = group.ToArray();
                var endpoint = group.First();

                // RoutePattern.Defaults contains the default values parsed from the template
                // as well as those specified with a literal. We need to separate those
                // for legacy cases.
                //
                // To do this we re-parse the original text and compare.
                var withoutDefaults = RoutePatternFactory.Parse(endpoint.RoutePattern.RawText);
                var defaults = new RouteValueDictionary(endpoint.RoutePattern.Defaults);
                for (var i = 0; i < withoutDefaults.Parameters.Count; i++)
                {
                    var parameter = withoutDefaults.Parameters[i];
                    if (parameter.Default != null)
                    {
                        defaults.Remove(parameter.Name);
                    }
                }

                routes.Add(new Route(
                    new SelectorRouter(selector, candidates),
                    endpoint.RoutePattern.RawText,
                    defaults,
                    new Dictionary<string, object>(),
                    new RouteValueDictionary(),
                    _constraintResolver));
            }

            return new RouteMatcher(routes);
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
                var context = (EndpointSelectorContext)routeContext.HttpContext.Features.Get<IEndpointFeature>();
                
                // This is needed due to a quirk of our tests - they reuse the endpoint feature
                // across requests.
                context.Endpoint = null;

                await _selector.SelectAsync(routeContext.HttpContext, context, new CandidateSet(_candidates, _values, _scores));
                if (context.Endpoint != null)
                {
                    routeContext.Handler = (_) => Task.CompletedTask;
                }
            }
        }
    }
}
