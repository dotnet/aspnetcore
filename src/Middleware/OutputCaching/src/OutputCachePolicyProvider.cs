// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCachePolicyProvider
{
    private readonly OutputCacheOptions _options;

    public OutputCachePolicyProvider(OutputCacheOptions options)
    {
        _options = options;
    }

    public bool HasPolicies(HttpContext httpContext)
    {
        if (_options.BasePolicies != null)
        {
            return true;
        }

        // Remove check
        if (httpContext.Features.Get<IOutputCacheFeature>()?.Policies.Any() ?? false)
        {
            return true;
        }

        if (httpContext.GetEndpoint()?.Metadata.GetMetadata<IOutputCachePolicy>() != null)
        {
            return true;
        }

        return false;
    }

    public async Task OnRequestAsync(OutputCacheContext context)
    {
        if (_options.BasePolicies != null)
        {
            foreach (var policy in _options.BasePolicies)
            {
                await policy.OnRequestAsync(context);
            }
        }

        var policiesMetadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IOutputCachePolicy>();

        if (policiesMetadata != null)
        {
            // TODO: Log only?

            if (context.HttpContext.Response.HasStarted)
            {
                throw new InvalidOperationException("Can't define output caching policies after headers have been sent to client.");
            }

            await policiesMetadata.OnRequestAsync(context);
        }
    }

    public async Task OnServeFromCacheAsync(OutputCacheContext context)
    {
        if (_options.BasePolicies != null)
        {
            foreach (var policy in _options.BasePolicies)
            {
                await policy.OnServeFromCacheAsync(context);
            }
        }

        // Apply response policies defined on the feature, e.g. from action attributes

        var responsePolicies = context.HttpContext.Features.Get<IOutputCacheFeature>()?.Policies;

        if (responsePolicies != null)
        {
            foreach (var policy in responsePolicies)
            {
                await policy.OnServeFromCacheAsync(context);
            }
        }

        var policiesMetadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IOutputCachePolicy>();

        if (policiesMetadata != null)
        {
            await policiesMetadata.OnServeFromCacheAsync(context);
        }
    }

    public async Task OnServeResponseAsync(OutputCacheContext context)
    {
        if (_options.BasePolicies != null)
        {
            foreach (var policy in _options.BasePolicies)
            {
                await policy.OnServeResponseAsync(context);
            }
        }

        // Apply response policies defined on the feature, e.g. from action attributes

        var responsePolicies = context.HttpContext.Features.Get<IOutputCacheFeature>()?.Policies;

        if (responsePolicies != null)
        {
            foreach (var policy in responsePolicies)
            {
                await policy.OnServeResponseAsync(context);
            }
        }

        var policiesMetadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IOutputCachePolicy>();

        if (policiesMetadata != null)
        {
            await policiesMetadata.OnServeResponseAsync(context);
        }
    }
}
