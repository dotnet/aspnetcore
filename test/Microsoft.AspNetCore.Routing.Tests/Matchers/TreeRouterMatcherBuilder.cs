// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class TreeRouterMatcherBuilder : MatcherBuilder
    {
        private readonly List<MatcherBuilderEntry> _entries;

        public TreeRouterMatcherBuilder()
        {
            _entries = new List<MatcherBuilderEntry>();
        }

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            _entries.Add(new MatcherBuilderEntry(endpoint));
        }

        public override Matcher Build()
        {
            _entries.Sort();

            var builder = new TreeRouteBuilder(
                NullLoggerFactory.Instance,
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                new DefaultInlineConstraintResolver(Options.Create(new RouteOptions())));

            var cache = new EndpointConstraintCache(
                new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>()),
                new[] { new DefaultEndpointConstraintProvider(), });
            var selector = new EndpointSelector(null, cache, NullLoggerFactory.Instance);

            var groups = _entries
                .GroupBy(e => (e.Order, e.Precedence, e.Endpoint.RoutePattern.RawText))
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
