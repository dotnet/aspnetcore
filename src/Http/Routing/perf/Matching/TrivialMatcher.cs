// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // A test-only matcher implementation - used as a baseline for simpler
    // perf tests. The idea with this matcher is that we can cheat on the requirements
    // to establish a lower bound for perf comparisons.
    internal sealed class TrivialMatcher : Matcher
    {
        private readonly RouteEndpoint _endpoint;
        private readonly Candidate[] _candidates;

        public TrivialMatcher(RouteEndpoint endpoint)
        {
            _endpoint = endpoint;

            _candidates = new Candidate[] { new Candidate(endpoint), };
        }

        public sealed override Task MatchAsync(HttpContext httpContext, EndpointSelectorContext context)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var path = httpContext.Request.Path.Value;
            if (string.Equals(_endpoint.RoutePattern.RawText, path, StringComparison.OrdinalIgnoreCase))
            {
                context.Endpoint = _endpoint;
                context.RouteValues = new RouteValueDictionary();
            }

            return Task.CompletedTask;
        }

        // This is here so this can be tested alongside DFA matcher.
        internal Candidate[] FindCandidateSet(string path, ReadOnlySpan<PathSegment> segments)
        {
            if (string.Equals(_endpoint.RoutePattern.RawText, path, StringComparison.OrdinalIgnoreCase))
            {
                return _candidates;
            }

            return Array.Empty<Candidate>();
        }
    }
}
