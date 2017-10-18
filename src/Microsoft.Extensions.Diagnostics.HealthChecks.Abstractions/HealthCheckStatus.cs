// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Represents the status of a health check result.
    /// </summary>
    /// <remarks>
    /// The values of this enum or ordered from least healthy to most healthy. So <see cref="HealthCheckStatus.Degraded"/> is
    /// greater than <see cref="HealthCheckStatus.Unhealthy"/> but less than <see cref="HealthCheckStatus.Healthy"/>.
    /// </remarks>
    public enum HealthCheckStatus
    {
        /// <summary>
        /// This value should not be returned by a health check. It is used to represent an uninitialized value.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// This value should not be returned by a health check. It is used to indicate that an unexpected exception was
        /// thrown when running the health check.
        /// </summary>
        Failed = 1,

        /// <summary>
        /// Indicates that the health check determined that the component was unhealthy.
        /// </summary>
        Unhealthy = 2,

        /// <summary>
        /// Indicates that the health check determined that the component was in a degraded state.
        /// </summary>
        Degraded = 3,

        /// <summary>
        /// Indicates that the health check determined that the component was healthy.
        /// </summary>
        Healthy = 4,
    }
}
