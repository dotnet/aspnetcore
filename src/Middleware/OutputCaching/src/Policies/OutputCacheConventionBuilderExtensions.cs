// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A set of endpoint extension methods.
/// </summary>
public static class OutputCacheConventionBuilderExtensions
{
    /// <summary>
    /// Marks an endpoint to be cached with the default policy.
    /// </summary>
    public static TBuilder CacheOutput<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Enable caching if this method is invoked on an endpoint, extra policies can disable it

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(DefaultPolicy.Instance);
        });
        return builder;
    }

    /// <summary>
    /// Marks an endpoint to be cached with the specified policy.
    /// </summary>
    public static TBuilder CacheOutput<TBuilder>(this TBuilder builder, IOutputCachePolicy policy) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Enable caching if this method is invoked on an endpoint, extra policies can disable it

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(policy);
        });
        return builder;
    }

    /// <summary>
    /// Marks an endpoint to be cached using the specified policy builder.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="policy">An action on <see cref="OutputCachePolicyBuilder"/>.</param>
    public static TBuilder CacheOutput<TBuilder>(this TBuilder builder, Action<OutputCachePolicyBuilder> policy)
        where TBuilder : IEndpointConventionBuilder
        => CacheOutput(builder, policy, false);

    /// <summary>
    /// Marks an endpoint to be cached using the specified policy builder.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="policy">An action on <see cref="OutputCachePolicyBuilder"/>.</param>
    /// <param name="excludeDefaultPolicy">Whether to exclude the default policy or not.</param>
    public static TBuilder CacheOutput<TBuilder>(this TBuilder builder, Action<OutputCachePolicyBuilder> policy, bool excludeDefaultPolicy) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var outputCachePolicyBuilder = new OutputCachePolicyBuilder(excludeDefaultPolicy);

        policy?.Invoke(outputCachePolicyBuilder);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(outputCachePolicyBuilder.Build());
        });

        return builder;
    }

    /// <summary>
    /// Marks an endpoint to be cached using a named policy.
    /// </summary>
    public static TBuilder CacheOutput<TBuilder>(this TBuilder builder, string policyName) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var policy = new NamedPolicy(policyName);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(policy);
        });

        return builder;
    }
}
