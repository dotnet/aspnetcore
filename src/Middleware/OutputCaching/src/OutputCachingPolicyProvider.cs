// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCachingPolicyProvider : IOutputCachingPolicyProvider
{
    private readonly OutputCachingOptions options;

    public OutputCachingPolicyProvider(IOptions<OutputCachingOptions> options)
    {
        this.options = options.Value;
    }

    public async Task OnRequestAsync(IOutputCachingContext context)
    {
        foreach (var policy in options.Policies)
        {
            await policy.OnRequestAsync(context);
        }

        var policiesMedata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPoliciesMetadata>();

        if (policiesMedata != null)
        {
            // TODO: Log only?

            if (context.HttpContext.Response.HasStarted)
            {
                throw new InvalidOperationException("Can't define output caching policies after headers have been sent to client.");
            }

            foreach (var policy in policiesMedata.Policies)
            {
                await policy.OnRequestAsync(context);
            }
        }
    }

    public async Task OnServeFromCacheAsync(IOutputCachingContext context)
    {
        foreach (var policy in options.Policies)
        {
            await policy.OnServeFromCacheAsync(context);
        }

        // Apply response policies defined on the feature, e.g. from action attributes

        var responsePolicies = context.HttpContext.Features.Get<IOutputCachingFeature>()?.Policies;

        if (responsePolicies != null)
        {
            foreach (var policy in responsePolicies)
            {
                await policy.OnServeFromCacheAsync(context);
            }
        }

        var policiesMedata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPoliciesMetadata>();

        if (policiesMedata != null)
        {
            foreach (var policy in policiesMedata.Policies)
            {
                await policy.OnServeFromCacheAsync(context);
            }
        }
    }

    public async Task OnServeResponseAsync(IOutputCachingContext context)
    {
        foreach (var policy in options.Policies)
        {
            await policy.OnServeResponseAsync(context);
        }

        // Apply response policies defined on the feature, e.g. from action attributes

        var responsePolicies = context.HttpContext.Features.Get<IOutputCachingFeature>()?.Policies;

        if (responsePolicies != null)
        {
            foreach (var policy in responsePolicies)
            {
                await policy.OnServeResponseAsync(context);
            }
        }

        var policiesMedata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPoliciesMetadata>();

        if (policiesMedata != null)
        {
            foreach (var policy in policiesMedata.Policies)
            {
                await policy.OnServeResponseAsync(context);
            }
        }
    }
}
