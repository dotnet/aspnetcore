// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

// A test-only matcher implementation - used as a baseline for more compilated
// perf tests. The idea with this matcher is that we can cheat on the requirements
// to establish a lower bound for perf comparisons.
internal class BarebonesMatcher : Matcher
{
    public readonly InnerMatcher[] Matchers;

    public BarebonesMatcher(InnerMatcher[] matchers)
    {
        Matchers = matchers;
    }

    public override Task MatchAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var path = httpContext.Request.Path.Value;
        for (var i = 0; i < Matchers.Length; i++)
        {
            if (Matchers[i].TryMatch(path))
            {
                httpContext.SetEndpoint(Matchers[i].Endpoint);
                httpContext.Request.RouteValues = new RouteValueDictionary();
            }
        }

        return Task.CompletedTask;
    }

    public sealed class InnerMatcher : Matcher
    {
        public readonly RouteEndpoint Endpoint;

        private readonly string[] _segments;
        private readonly Candidate[] _candidates;

        public InnerMatcher(string[] segments, RouteEndpoint endpoint)
        {
            _segments = segments;
            Endpoint = endpoint;

            _candidates = new Candidate[] { new Candidate(endpoint), };
        }

        public bool TryMatch(string path)
        {
            var segment = 0;

            var start = 1; // PathString always has a leading slash
            var end = 0;
            while ((end = path.IndexOf('/', start)) >= 0)
            {
                var comparand = _segments.Length > segment ? _segments[segment] : null;
                if ((comparand == null && end - start == 0) ||
                    (comparand != null &&
                        (comparand.Length != end - start ||
                        string.Compare(
                            path,
                            start,
                            comparand,
                            0,
                            comparand.Length,
                            StringComparison.OrdinalIgnoreCase) != 0)))
                {
                    return false;
                }

                start = end + 1;
                segment++;
            }

            // residue
            var length = path.Length - start;
            if (length > 0)
            {
                var comparand = _segments.Length > segment ? _segments[segment] : null;
                if (comparand != null &&
                    (comparand.Length != length ||
                    string.Compare(
                        path,
                        start,
                        comparand,
                        0,
                        comparand.Length,
                        StringComparison.OrdinalIgnoreCase) != 0))
                {
                    return false;
                }

                segment++;
            }

            return segment == _segments.Length;
        }

        internal Candidate[] FindCandidateSet(string path, ReadOnlySpan<PathSegment> segments)
        {
            if (TryMatch(path))
            {
                return _candidates;
            }

            return Array.Empty<Candidate>();
        }

        public override Task MatchAsync(HttpContext httpContext)
        {
            if (TryMatch(httpContext.Request.Path.Value))
            {
                httpContext.SetEndpoint(Endpoint);
                httpContext.Request.RouteValues = new RouteValueDictionary();
            }

            return Task.CompletedTask;
        }
    }
}
