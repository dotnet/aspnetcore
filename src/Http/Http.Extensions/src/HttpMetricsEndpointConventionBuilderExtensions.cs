// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// HTTP metrics extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class HttpMetricsEndpointConventionBuilderExtensions
{
    private static readonly DisableHttpMetricsAttribute _disableHttpMetricsAttribute = new DisableHttpMetricsAttribute();

    /// <summary>
    /// Specifies that HTTP request duration metrics is disabled for an endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">The type of endpoint convention builder.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder DisableHttpMetrics<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(b => b.Metadata.Add(_disableHttpMetricsAttribute));
        return builder;
    }
}
