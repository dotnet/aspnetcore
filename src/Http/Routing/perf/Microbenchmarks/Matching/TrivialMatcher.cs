// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

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

    public sealed override Task MatchAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var path = httpContext.Request.Path.Value;
        if (string.Equals(_endpoint.RoutePattern.RawText, path, StringComparison.OrdinalIgnoreCase))
        {
            httpContext.SetEndpoint(_endpoint);
            httpContext.Request.RouteValues = new RouteValueDictionary();
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
