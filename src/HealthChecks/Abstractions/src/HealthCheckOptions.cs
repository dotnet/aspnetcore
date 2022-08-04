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
    public HealthCheckOptions(TimeSpan? delay = default, TimeSpan? period = default)
    {
        Delay = delay;
        Period = period;
    }

    /// <summary>
    /// Gets the initial individual delay applied to the
    /// individual HealthCheck after the application starts before executing.
    /// The delay is applied once at startup, and does
    /// not apply to subsequent iterations.
    /// </summary>
    public TimeSpan? Delay { get; }

    /// <summary>
    /// Gets or sets the individual period used for the check.
    /// </summary>
    public TimeSpan? Period { get; }

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
               EqualityComparer<TimeSpan?>.Default.Equals(Period, other.Period);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = -1368980644;
        hashCode = hashCode * -1521134295 + Delay.GetHashCode();
        hashCode = hashCode * -1521134295 + Period.GetHashCode();
        return hashCode;
    }
}
