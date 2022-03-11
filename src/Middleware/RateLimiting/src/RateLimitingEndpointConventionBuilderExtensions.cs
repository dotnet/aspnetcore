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
    /// <param name="name">The name of the RateLimiter to add to the endpoint.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireRateLimiting<TBuilder>(this TBuilder builder, String name) where TBuilder : IEndpointConventionBuilder
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new RateLimitingPolicyMetadata(name));
        });

        return builder;
    }
}
