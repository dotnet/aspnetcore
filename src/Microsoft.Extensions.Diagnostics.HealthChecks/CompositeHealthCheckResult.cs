// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Represents the results of multiple health checks.
    /// </summary>
    public class CompositeHealthCheckResult
    {
        /// <summary>
        /// A <see cref="IReadOnlyDictionary{TKey, T}"/> containing the results from each health check.
        /// </summary>
        /// <remarks>
        /// The keys in this dictionary map to the name of the health check, the values are the <see cref="HealthCheckResult"/>
        /// returned when <see cref="IHealthCheck.CheckHealthAsync(System.Threading.CancellationToken)"/> was called for that health check.
        /// </remarks>
        public IReadOnlyDictionary<string, HealthCheckResult> Results { get; }

        /// <summary>
        /// Gets a <see cref="HealthCheckStatus"/> representing the aggregate status of all the health checks.
        /// </summary>
        /// <remarks>
        /// This value is determined by taking the "worst" result of all the results. So if any result is <see cref="HealthCheckStatus.Failed"/>,
        /// this value is <see cref="HealthCheckStatus.Failed"/>. If no result is <see cref="HealthCheckStatus.Failed"/> but any result is
        /// <see cref="HealthCheckStatus.Unhealthy"/>, this value is <see cref="HealthCheckStatus.Unhealthy"/>, etc.
        /// </remarks>
        public HealthCheckStatus Status { get; }

        /// <summary>
        /// Create a new <see cref="CompositeHealthCheckResult"/> from the specified results.
        /// </summary>
        /// <param name="results">A <see cref="IReadOnlyDictionary{TKey, T}"/> containing the results from each health check.</param>
        public CompositeHealthCheckResult(IReadOnlyDictionary<string, HealthCheckResult> results)
        {
            Results = results;
            Status = CalculateAggregateStatus(results.Values);
        }

        private HealthCheckStatus CalculateAggregateStatus(IEnumerable<HealthCheckResult> results)
        {
            // This is basically a Min() check, but we know the possible range, so we don't need to walk the whole list
            var currentValue = HealthCheckStatus.Healthy;
            foreach (var result in results)
            {
                if (currentValue > result.Status)
                {
                    currentValue = result.Status;
                }

                if (currentValue == HealthCheckStatus.Failed)
                {
                    // Game over, man! Game over!
                    // (We hit the worst possible status, so there's no need to keep iterating)
                    return currentValue;
                }
            }

            return currentValue;
        }
    }
}
