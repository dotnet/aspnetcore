// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to register health checks.
/// </summary>
public interface IHealthChecksBuilder
{
    /// <summary>
    /// Adds a <see cref="HealthCheckRegistration"/> for a health check.
    /// </summary>
    /// <param name="registration">The <see cref="HealthCheckRegistration"/>.</param>
    IHealthChecksBuilder Add(HealthCheckRegistration registration);

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> into which <see cref="IHealthCheck"/> instances should be registered.
    /// </summary>
    IServiceCollection Services { get; }
}
