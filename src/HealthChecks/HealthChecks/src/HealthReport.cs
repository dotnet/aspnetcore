// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represents the result of executing a group of <see cref="IHealthCheck"/> instances.
/// </summary>
public sealed class HealthReport
{
    /// <summary>
    /// Create a new <see cref="HealthReport"/> from the specified results.
    /// </summary>
    /// <param name="entries">A <see cref="IReadOnlyDictionary{TKey, T}"/> containing the results from each health check.</param>
    /// <param name="totalDuration">A value indicating the time the health check service took to execute.</param>
    public HealthReport(IReadOnlyDictionary<string, HealthReportEntry> entries, TimeSpan totalDuration)
        : this(
            entries,
            CalculateAggregateStatus(entries?.Values ?? throw new ArgumentNullException(nameof(entries))),
            totalDuration)
    {
    }

    /// <summary>
    /// Create a new <see cref="HealthReport"/> from the specified results.
    /// </summary>
    /// <param name="entries">A <see cref="IReadOnlyDictionary{TKey, T}"/> containing the results from each health check.</param>
    /// <param name="status">A <see cref="HealthStatus"/> representing the aggregate status of all the health checks.</param>
    /// <param name="totalDuration">A value indicating the time the health check service took to execute.</param>
    public HealthReport(IReadOnlyDictionary<string, HealthReportEntry> entries, HealthStatus status, TimeSpan totalDuration)
    {
        Entries = entries;
        Status = status;
        TotalDuration = totalDuration;
    }

    /// <summary>
    /// A <see cref="IReadOnlyDictionary{TKey, T}"/> containing the results from each health check.
    /// </summary>
    /// <remarks>
    /// The keys in this dictionary map the name of each executed health check to a <see cref="HealthReportEntry"/> for the
    /// result data returned from the corresponding health check.
    /// </remarks>
    public IReadOnlyDictionary<string, HealthReportEntry> Entries { get; }

    /// <summary>
    /// Gets a <see cref="HealthStatus"/> representing the aggregate status of all the health checks. The value of <see cref="Status"/>
    /// will be the most severe status reported by a health check. If no checks were executed, the value is always <see cref="HealthStatus.Healthy"/>.
    /// </summary>
    public HealthStatus Status { get; }

    /// <summary>
    /// Gets the time the health check service took to execute.
    /// </summary>
    public TimeSpan TotalDuration { get; }

    private static HealthStatus CalculateAggregateStatus(IEnumerable<HealthReportEntry> entries)
    {
        // This is basically a Min() check, but we know the possible range, so we don't need to walk the whole list
        var currentValue = HealthStatus.Healthy;
        foreach (var entry in entries)
        {
            if (currentValue > entry.Status)
            {
                currentValue = entry.Status;
            }

            if (currentValue == HealthStatus.Unhealthy)
            {
                // Game over, man! Game over!
                // (We hit the worst possible status, so there's no need to keep iterating)
                return currentValue;
            }
        }

        return currentValue;
    }
}
