// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring the <see cref="HealthCheckOptions"/>
/// consumed by the health check middleware from an <see cref="IHealthChecksBuilder"/>.
/// </summary>
public static class HealthChecksBuilderConfigureOptionsExtensions
{
    /// <summary>
    /// Configures the <see cref="HealthCheckOptions"/> used by the health check middleware.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="configureOptions">A delegate that is used to configure the <see cref="HealthCheckOptions"/>.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
    /// <remarks>
    /// This is a convenience wrapper around
    /// <c>builder.Services.Configure&lt;HealthCheckOptions&gt;(configureOptions)</c> that lets the
    /// middleware options be configured in the same expression that registers the
    /// <see cref="HealthCheckService"/> and any individual health checks. Multiple invocations
    /// are additive via the standard <c>Microsoft.Extensions.Options</c> pipeline.
    /// </remarks>
    public static IHealthChecksBuilder ConfigureHealthCheckOptions(
        this IHealthChecksBuilder builder,
        Action<HealthCheckOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services.Configure(configureOptions);
        return builder;
    }
}
