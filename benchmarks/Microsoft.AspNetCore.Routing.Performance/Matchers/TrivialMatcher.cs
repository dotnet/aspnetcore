// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // A test-only matcher implementation - used as a baseline for simpler
    // perf tests. The idea with this matcher is that we can cheat on the requirements
    // to establish a lower bound for perf comparisons.
    internal class TrivialMatcher : Matcher
    {
        private readonly MatcherEndpoint _endpoint;

        public TrivialMatcher(MatcherEndpoint endpoint)
        {
            _endpoint = endpoint;
        }

        public override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            var path = httpContext.Request.Path.Value;
            if (string.Equals(_endpoint.Template, path, StringComparison.OrdinalIgnoreCase))
            {
                feature.Endpoint = _endpoint;
                feature.Values = new RouteValueDictionary();
            }

            return Task.CompletedTask;
        }
    }
}
