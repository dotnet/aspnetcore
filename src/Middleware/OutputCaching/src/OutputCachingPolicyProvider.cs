// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCachingPolicyProvider : IOutputCachingPolicyProvider
{
    private readonly OutputCachingOptions _options;

    public OutputCachingPolicyProvider(IOptions<OutputCachingOptions> options)
    {
        _options = options.Value;
    }

    // Not in interface
    public bool HasPolicies(HttpContext httpContext)
    {
        if (_options.BasePolicies != null)
        {
            return true;
        }

        // Remove check
        if (httpContext.Features.Get<IOutputCachingFeature>()?.Policies.Any() ?? false)
        {
            return true;
        }

        if (httpContext.GetEndpoint()?.Metadata.GetMetadata<IPoliciesMetadata>()?.Policy != null)
        {
            return true;
        }

        return false;
    }

    public async Task OnRequestAsync(OutputCachingContext context)
    {
        if (_options.BasePolicies != null)
        {
            foreach (var policy in _options.BasePolicies)
            {
                await policy.OnRequestAsync(context);
            }
        }

        var policiesMetadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPoliciesMetadata>();

        if (policiesMetadata != null)
        {
            // TODO: Log only?

            if (context.HttpContext.Response.HasStarted)
            {
                throw new InvalidOperationException("Can't define output caching policies after headers have been sent to client.");
            }

            await policiesMetadata.Policy.OnRequestAsync(context);
        }
    }

    public async Task OnServeFromCacheAsync(OutputCachingContext context)
    {
        if (_options.BasePolicies != null)
        {
            foreach (var policy in _options.BasePolicies)
            {
                await policy.OnServeFromCacheAsync(context);
            }
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

        var policiesMetadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPoliciesMetadata>();

        if (policiesMetadata != null)
        {
            await policiesMetadata.Policy.OnServeFromCacheAsync(context);
        }
    }

    public async Task OnServeResponseAsync(OutputCachingContext context)
    {
        if (_options.BasePolicies != null)
        {
            foreach (var policy in _options.BasePolicies)
            {
                await policy.OnServeResponseAsync(context);
            }
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

        var policiesMetadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPoliciesMetadata>();

        if (policiesMetadata != null)
        {
            await policiesMetadata.Policy.OnServeResponseAsync(context);
        }
    }
}
