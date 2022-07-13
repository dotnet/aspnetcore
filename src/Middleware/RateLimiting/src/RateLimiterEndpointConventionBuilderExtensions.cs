// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.RateLimiting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Rate limiter extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class RateLimiterEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds the specified rate limiting policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policyName">The name of the rate limiting policy to add to the endpoint.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireRateLimiting<TBuilder>(this TBuilder builder, string policyName) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        ArgumentNullException.ThrowIfNull(policyName);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new RateLimiterMetadata(policyName));
        });

        return builder;
    }
}
