// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class DataSourceDependentMatcher : Matcher
    {
        private readonly Func<MatcherBuilder> _matcherBuilderFactory;
        private readonly DataSourceDependentCache<Matcher> _cache;

        public DataSourceDependentMatcher(
            EndpointDataSource dataSource,
            Func<MatcherBuilder> matcherBuilderFactory)
        {
            _matcherBuilderFactory = matcherBuilderFactory;

            _cache = new DataSourceDependentCache<Matcher>(dataSource, CreateMatcher);
            _cache.EnsureInitialized();
        }

        // Used in tests
        internal Matcher CurrentMatcher => _cache.Value;

        public override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            return CurrentMatcher.MatchAsync(httpContext, feature);
        }

        private Matcher CreateMatcher(IReadOnlyList<Endpoint> endpoints)
        {
            var builder = _matcherBuilderFactory();
            for (var i = 0; i < endpoints.Count; i++)
            {
                // By design we only look at MatcherEndpoint here. It's possible to
                // register other endpoint types, which are non-routable, and it's
                // ok that we won't route to them.
                var endpoint = endpoints[i] as MatcherEndpoint;
                if (endpoint != null)
                {
                    builder.AddEndpoint(endpoint);
                }
            }

            return builder.Build();
        }
    }
}
