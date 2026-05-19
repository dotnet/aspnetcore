// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check context. Provides health check registrations to <see cref="IHealthCheck.CheckHealthAsync(HealthCheckContext, System.Threading.CancellationToken)"/>.
/// </summary>
public sealed class HealthCheckContext
{
    /// <summary>
    /// Gets or sets the <see cref="HealthCheckRegistration"/> of the currently executing <see cref="IHealthCheck"/>.
    /// </summary>
    // This allows null values for convenience during unit testing. This is expected to be non-null when within application code.
    public HealthCheckRegistration Registration { get; set; } = default!;
}
