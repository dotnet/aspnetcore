// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class BlazorWebEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    // Resolve framework ties only after policies that determine request eligibility.
    public override int Order => int.MaxValue;

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        => endpoints.Any(endpoint => endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is not null);

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i) || GetFrameworkEndpoint(candidates[i].Endpoint) is not { } endpoint)
            {
                continue;
            }

            for (var j = i + 1; j < candidates.Count; j++)
            {
                if (candidates.IsValidCandidate(j) &&
                    candidates[i].Score == candidates[j].Score &&
                    GetFrameworkEndpoint(candidates[j].Endpoint) is { } other &&
                    endpoint.RequestDelegate == other.RequestDelegate &&
                    string.Equals(endpoint.RoutePattern.RawText, other.RoutePattern.RawText, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.SetValidity(j, false);
                }
            }
        }

        return Task.CompletedTask;
    }

    private static RouteEndpoint? GetFrameworkEndpoint(Endpoint endpoint)
    {
        if (endpoint is RouteEndpoint routeEndpoint &&
            endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is { } metadata &&
            endpoint.RequestDelegate == metadata.RequestDelegate)
        {
            return routeEndpoint;
        }

        return null;
    }
}
