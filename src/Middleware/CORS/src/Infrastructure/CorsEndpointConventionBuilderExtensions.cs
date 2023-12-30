// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// CORS extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class CorsEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds a CORS policy with the default policy name to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireCors<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new EnableCorsAttribute());
        });
        return builder;
    }

    /// <summary>
    /// Adds a CORS policy with the specified name to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policyName">The CORS policy name.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireCors<TBuilder>(this TBuilder builder, string policyName) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new EnableCorsAttribute(policyName));
        });
        return builder;
    }

    /// <summary>
    /// Adds the specified CORS policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="configurePolicy">A delegate which can use a policy builder to build a policy.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireCors<TBuilder>(this TBuilder builder, Action<CorsPolicyBuilder> configurePolicy) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurePolicy);

        var policyBuilder = new CorsPolicyBuilder();
        configurePolicy(policyBuilder);
        var policy = policyBuilder.Build();

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new CorsPolicyMetadata(policy));
        });
        return builder;
    }
}
