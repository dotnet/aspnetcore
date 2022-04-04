// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.OutputCaching.Policies;
public static class PolicyExtensions
{
    public static TBuilder OutputCache<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var policiesMetadata = new PoliciesMetadata();

        // Enable caching if this method is invoked on an endpoint, extra policies can disable it
        policiesMetadata.Policies.Add(EnableCachingPolicy.Instance);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(policiesMetadata);
        });
        return builder;
    }

    public static TBuilder OutputCache<TBuilder>(this TBuilder builder, params IOutputCachingPolicy[] items) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        var policiesMetadata = new PoliciesMetadata();

        // Enable caching if this method is invoked on an endpoint, extra policies can disable it
        policiesMetadata.Policies.Add(EnableCachingPolicy.Instance);

        policiesMetadata.Policies.AddRange(items);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(policiesMetadata);
        });
        return builder;
    } 

    public static TBuilder OutputCache<TBuilder>(this TBuilder builder, Action<OutputCachePolicyBuilder> policy) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var outputCachePolicyBuilder = new OutputCachePolicyBuilder();
        policy?.Invoke(outputCachePolicyBuilder);

        var policiesMetadata = new PoliciesMetadata();

        // Enable caching if this method is invoked on an endpoint, extra policies can disable it
        policiesMetadata.Policies.Add(EnableCachingPolicy.Instance);

        policiesMetadata.Policies.Add(outputCachePolicyBuilder.Build());

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(policiesMetadata);
        });

        return builder;
    }
}
