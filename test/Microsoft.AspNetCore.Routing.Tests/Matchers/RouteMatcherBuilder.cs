// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class RouteMatcherBuilder : MatcherBuilder
    {
        private readonly IInlineConstraintResolver _constraintResolver;
        private readonly List<Entry> _entries;

        public RouteMatcherBuilder()
        {
            _constraintResolver = new DefaultInlineConstraintResolver(Options.Create(new RouteOptions()));
            _entries = new List<Entry>();
        }

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            var handler = new RouteHandler(c =>
            {
                c.Features.Get<IEndpointFeature>().Endpoint = endpoint;
                return Task.CompletedTask;
            });

            // MatcherEndpoint.Values contains the default values parsed from the template
            // as well as those specified with a literal. We need to separate those
            // for legacy cases.
            var defaults = new RouteValueDictionary(endpoint.Values);
            for (var i = 0; i < endpoint.ParsedTemlate.Parameters.Count; i++)
            {
                var parameter = endpoint.ParsedTemlate.Parameters[i];
                if (parameter.DefaultValue != null)
                {
                    defaults.Remove(parameter.Name);
                }
            }

            _entries.Add(new Entry()
            {
                Endpoint = endpoint,
                Route = new Route(
                    handler,
                    endpoint.Template,
                    defaults,
                    new Dictionary<string, object>(),
                    new RouteValueDictionary(),
                    _constraintResolver),
            });
        }

        public override Matcher Build()
        {
            _entries.Sort();
            var routes = new RouteCollection();
            for (var i = 0; i < _entries.Count; i++)
            {
                routes.Add(_entries[i].Route);
            }

            return new RouteMatcher(routes);
        }

        private struct Entry : IComparable<Entry>
        {
            public MatcherEndpoint Endpoint;
            public Route Route;

            public int CompareTo(Entry other)
            {
                var comparison = Endpoint.Order.CompareTo(other.Endpoint.Order);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = RoutePrecedence.ComputeInbound(Endpoint.ParsedTemlate).CompareTo(RoutePrecedence.ComputeInbound(other.Endpoint.ParsedTemlate));
                if (comparison != 0)
                {
                    return comparison;
                }

                return Endpoint.Template.CompareTo(other.Endpoint.Template);
            }
        }
    }
}
