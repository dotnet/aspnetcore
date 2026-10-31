// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Timeouts;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Request timeouts extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class RequestTimeoutsIEndpointConventionBuilderExtensions
{
    private static readonly DisableRequestTimeoutAttribute _disableRequestTimeoutAttribute = new DisableRequestTimeoutAttribute();

    /// <summary>
    /// Specifies a timeout for the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="timeout">The timeout to apply for the endpoint(s).</param>
    /// <returns>The original convention builder parameter.</returns>
    public static IEndpointConventionBuilder WithRequestTimeout(this IEndpointConventionBuilder builder, TimeSpan timeout)
    {
        return builder.WithRequestTimeout(new RequestTimeoutPolicy
        {
            Timeout = timeout
        });
    }

    /// <summary>
    /// Specifies a timeout policy for to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policyName">The name (case-insensitive) of the policy to apply for the endpoint(s).</param>
    /// <returns>The original convention builder parameter.</returns>
    public static IEndpointConventionBuilder WithRequestTimeout(this IEndpointConventionBuilder builder, string policyName)
    {
        builder.Add(b => b.Metadata.Add(new RequestTimeoutAttribute(policyName)));
        return builder;
    }

    /// <summary>
    /// Specifies a timeout policy for to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policy">The request timeout policy.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static IEndpointConventionBuilder WithRequestTimeout(this IEndpointConventionBuilder builder, RequestTimeoutPolicy policy)
    {
        builder.Add(b => b.Metadata.Add(policy));
        return builder;
    }

    /// <summary>
    /// Disables request timeout on the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The original convention builder parameter.</returns>
    /// <remarks>Will skip both the default timeout, and any endpoint-specific timeout that apply to the endpoint(s).</remarks>
    public static IEndpointConventionBuilder DisableRequestTimeout(this IEndpointConventionBuilder builder)
    {
        builder.Add(b => b.Metadata.Add(_disableRequestTimeoutAttribute));
        return builder;
    }
}
