// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using static Microsoft.AspNetCore.Routing.Matchers.BarebonesMatcher;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class BarebonesMatcherBuilder : MatcherBuilder
    {
        private List<MatcherEndpoint> _endpoints = new List<MatcherEndpoint>();

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public override Matcher Build()
        {
            var matchers = new InnerMatcher[_endpoints.Count];
            for (var i = 0; i < _endpoints.Count; i++)
            {
                var endpoint = _endpoints[i];
                var pathSegments = endpoint.RoutePattern.PathSegments
                    .Select(s => s.IsSimple && s.Parts[0] is RoutePatternLiteralPart literalPart ? literalPart.Content : null)
                    .ToArray();
                matchers[i] = new InnerMatcher(pathSegments, _endpoints[i]);
            }

            return new BarebonesMatcher(matchers);
        }
    }
}
