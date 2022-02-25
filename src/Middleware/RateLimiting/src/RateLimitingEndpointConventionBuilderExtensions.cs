// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.AspNetCore.RateLimiting.Policies;

namespace Microsoft.AspNetCore.Builder;
public static class RateLimitingEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds the specified Rate Limiting policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="configurePolicy">A delegate which can use a policy builder to build a policy.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireRateLimiting<TBuilder>(this TBuilder builder, Action<RateLimitingPolicyBuilder> configurePolicy) where TBuilder : IEndpointConventionBuilder
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configurePolicy == null)
        {
            throw new ArgumentNullException(nameof(configurePolicy));
        }

        var policyBuilder = new RateLimitingPolicyBuilder();
        configurePolicy(policyBuilder);
        var policy = policyBuilder.Build();

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new RateLimitingPolicyMetadata(policy));
        });
        return builder;
    }
}
