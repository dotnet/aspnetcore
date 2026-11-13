// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class BlazorWebEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    // Preserve host, method, and other routing policies before resolving framework endpoint ties.
    public override int Order => int.MaxValue;

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        var count = 0;
        foreach (var endpoint in endpoints)
        {
            if (endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is not null && ++count > 1)
            {
                return true;
            }
        }

        return false;
    }

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i) ||
                candidates[i].Endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is null)
            {
                continue;
            }

            for (var j = i + 1; j < candidates.Count; j++)
            {
                if (candidates.IsValidCandidate(j) &&
                    candidates[i].Score == candidates[j].Score &&
                    candidates[i].Endpoint.RequestDelegate == candidates[j].Endpoint.RequestDelegate &&
                    candidates[j].Endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is not null)
                {
                    candidates.SetValidity(j, false);
                }
            }
        }

        return Task.CompletedTask;
    }
}

internal sealed class BlazorWebEndpointMetadata
{
    public static readonly BlazorWebEndpointMetadata Instance = new();

    private BlazorWebEndpointMetadata()
    {
    }
}
