// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matching;

internal class RouteMatcherBuilder : MatcherBuilder
{
    private readonly IInlineConstraintResolver _constraintResolver;
    private readonly List<RouteEndpoint> _endpoints;

    public RouteMatcherBuilder()
    {
        var routeOptions = new RouteOptions();
        routeOptions.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
        _constraintResolver = new DefaultInlineConstraintResolver(Options.Create(routeOptions), new TestServiceProvider());
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
