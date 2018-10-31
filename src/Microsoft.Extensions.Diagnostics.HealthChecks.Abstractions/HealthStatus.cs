// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Represents the reported status of a health check result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A status of <see cref="Unhealthy"/> should be considered the default value for a failing health check. Application
    /// developers may configure a health check to report a different status as desired.
    /// </para>
    /// <para>
    /// The values of this enum or ordered from least healthy to most healthy. So <see cref="HealthStatus.Degraded"/> is
    /// greater than <see cref="HealthStatus.Unhealthy"/> but less than <see cref="HealthStatus.Healthy"/>.
    /// </para>
    /// </remarks>
    public enum HealthStatus
    {
        /// <summary>
        /// Indicates that the health check determined that the component was unhealthy, or an unhandled
        /// exception was thrown while executing the health check.
        /// </summary>
        Unhealthy = 0,

        /// <summary>
        /// Indicates that the health check determined that the component was in a degraded state.
        /// </summary>
        Degraded = 1,

        /// <summary>
        /// Indicates that the health check determined that the component was healthy.
        /// </summary>
        Healthy = 2,
    }
}
