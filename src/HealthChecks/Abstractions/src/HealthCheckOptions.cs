// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represent the individual health check options associated with an <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration"/>.
/// </summary>
public class HealthCheckOptions : IEquatable<HealthCheckOptions?>
{

    /// <summary>
    /// Creates a new <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckOptions" />.
    /// </summary>
    /// <param name="delay">An optional <see cref="TimeSpan"/> representing the initial delay applied after the application starts before executing the check.</param>
    /// <param name="period">An optional <see cref="TimeSpan"/> representing the individual period of the check.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the individual timeout of the check.</param>
    public HealthCheckOptions(TimeSpan? delay = default, TimeSpan? period = default, TimeSpan? timeout = default)
    {
        Delay = delay;
        Period = period;
        Timeout = timeout;
    }

    /// <summary>
    /// Gets the initial individual delay applied to the
    /// individual HealthCheck after the application starts before executing.
    /// The delay is applied once at startup, and does
    /// not apply to subsequent iterations.
    /// </summary>
    public TimeSpan? Delay { get; }

    /// <summary>
    /// Gets the individual period used for the check.
    /// </summary>
    public TimeSpan? Period { get; }

    /// <summary>
    /// Gets the timeout for executing the health check.
    /// Use <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to execute with no timeout.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as HealthCheckOptions);
    }

    /// <inheritdoc/>
    public bool Equals(HealthCheckOptions? other)
    {
        return other is not null &&
               EqualityComparer<TimeSpan?>.Default.Equals(Delay, other.Delay) &&
               EqualityComparer<TimeSpan?>.Default.Equals(Period, other.Period) &&
               EqualityComparer<TimeSpan?>.Default.Equals(Timeout, other.Timeout);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = 251827172;
        hashCode = hashCode * -1521134295 + Delay.GetHashCode();
        hashCode = hashCode * -1521134295 + Period.GetHashCode();
        hashCode = hashCode * -1521134295 + Timeout.GetHashCode();
        return hashCode;
    }
}
