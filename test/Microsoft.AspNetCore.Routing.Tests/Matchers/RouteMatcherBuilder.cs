// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class RouteMatcherBuilder : MatcherBuilder
    {
        private readonly IInlineConstraintResolver _constraintResolver;
        private readonly List<MatcherBuilderEntry> _entries;

        public RouteMatcherBuilder()
        {
            _constraintResolver = new DefaultInlineConstraintResolver(Options.Create(new RouteOptions()));
            _entries = new List<MatcherBuilderEntry>();
        }

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            _entries.Add(new MatcherBuilderEntry(endpoint));
        }

        public override Matcher Build()
        {
            _entries.Sort();

            var cache = new EndpointConstraintCache(
                new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>()),
                new[] { new DefaultEndpointConstraintProvider(), });
            var selector = new EndpointSelector(null, cache, NullLoggerFactory.Instance);

            var groups = _entries
                .GroupBy(e => (e.Order, e.Precedence, e.Endpoint.Template))
                .OrderBy(g => g.Key.Order)
                .ThenBy(g => g.Key.Precedence);

            var routes = new RouteCollection();

            foreach (var group in groups)
            {
                var candidates = group.Select(e => e.Endpoint).ToArray();

                // MatcherEndpoint.Values contains the default values parsed from the template
                // as well as those specified with a literal. We need to separate those
                // for legacy cases.
                var endpoint = group.First().Endpoint;
                var defaults = new RouteValueDictionary(endpoint.Defaults);
                for (var i = 0; i < endpoint.ParsedTemplate.Parameters.Count; i++)
                {
                    var parameter = endpoint.ParsedTemplate.Parameters[i];
                    if (parameter.DefaultValue != null)
                    {
                        defaults.Remove(parameter.Name);
                    }
                }

                routes.Add(new Route(
                    new SelectorRouter(selector, candidates),
                    endpoint.Template,
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
            private readonly Endpoint[] _candidates;

            public SelectorRouter(EndpointSelector selector, Endpoint[] candidates)
            {
                _selector = selector;
                _candidates = candidates;
            }

            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                throw new NotImplementedException();
            }

            public Task RouteAsync(RouteContext context)
            {
                var endpoint = _selector.SelectBestCandidate(context.HttpContext, _candidates);
                if (endpoint != null)
                {
                    context.HttpContext.Features.Get<IEndpointFeature>().Endpoint = endpoint;
                    context.Handler = (_) => Task.CompletedTask;
                }
                return Task.CompletedTask;
            }
        }
    }
}
