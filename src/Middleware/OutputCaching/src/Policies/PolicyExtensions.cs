// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.OutputCaching.Policies;

/// <summary>
/// A set of endpoint extension methods.
/// </summary>
public static class PolicyExtensions
{
    /// <summary>
    /// Marks an endpoint to be cached.
    /// </summary>
    public static TBuilder CacheOutput<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var policiesMetadata = new PoliciesMetadata();

        // Enable caching if this method is invoked on an endpoint, extra policies can disable it
        policiesMetadata.Add(EnableCachingPolicy.Instance);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(policiesMetadata);
        });
        return builder;
    }

    /// <summary>
    /// Marks an endpoint to be cached with the specified policies.
    /// </summary>
    public static TBuilder CacheOutput<TBuilder>(this TBuilder builder, params IOutputCachingPolicy[] items) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(items);

        var policiesMetadata = new PoliciesMetadata();

        // Enable caching if this method is invoked on an endpoint, extra policies can disable it
        policiesMetadata.Add(EnableCachingPolicy.Instance);

        foreach (var item in items)
        {
            policiesMetadata.Add(item);
        }

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(policiesMetadata);
        });
        return builder;
    }

    /// <summary>
    /// Marks an endpoint to be cached with the specified policy.
    /// </summary>
    public static TBuilder CacheOutput<TBuilder>(this TBuilder builder, Action<OutputCachePolicyBuilder> policy) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var outputCachePolicyBuilder = new OutputCachePolicyBuilder();
        policy?.Invoke(outputCachePolicyBuilder);

        var policiesMetadata = new PoliciesMetadata();

        // Enable caching if this method is invoked on an endpoint, extra policies can disable it
        policiesMetadata.Add(EnableCachingPolicy.Instance);

        policiesMetadata.Add(outputCachePolicyBuilder.Build());

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(policiesMetadata);
        });

        return builder;
    }
}
