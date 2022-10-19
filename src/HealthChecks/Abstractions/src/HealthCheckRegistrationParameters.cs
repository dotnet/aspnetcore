// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represent the individual health check parameters associated with an <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration"/>.
/// </summary>
public class HealthCheckRegistrationParameters
{
    /// <summary>
    /// Creates a new <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistrationsParameters" />.
    /// </summary>
    /// <param name="delay">An optional <see cref="TimeSpan"/> representing the individual delay applied after the application starts before executing the check.</param>
    /// <param name="period">An optional <see cref="TimeSpan"/> representing the individual period of the check.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the individual timeout of the check.</param>
    /// <param name="isEnabled">An optional <see cref="bool"/> representing whether the health check should be run.</param>
    public HealthCheckRegistrationParameters(TimeSpan? delay = default, TimeSpan? period = default, TimeSpan? timeout = default, bool isEnabled = true)
    {
        Delay = delay;
        Period = period;
        Timeout = timeout;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Gets or sets the individual delay applied to the health check after the application starts before executing
    /// <see cref="IHealthCheckPublisher"/> instances. The delay is applied once at startup, and does
    /// not apply to subsequent iterations.
    /// </summary>
    public TimeSpan? Delay { get; }

    /// <summary>
    /// Gets the individual period used for the check.
    /// </summary>
    public TimeSpan? Period { get; }

    /// <summary>
    /// Gets the individual timeout for executing the health check.
    /// Use <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to execute with no timeout.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
	/// Gets or sets whether the health check should be run. Enabled by default.
	/// </summary>
	public bool IsEnabled { get; set; }
}
