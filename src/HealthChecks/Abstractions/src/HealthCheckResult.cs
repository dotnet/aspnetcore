// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// Represents the result of a health check.
    /// </summary>
    public struct HealthCheckResult
    {
        private static readonly IReadOnlyDictionary<string, object> _emptyReadOnlyDictionary = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new <see cref="HealthCheckResult"/> with the specified values for <paramref name="status"/>, 
        /// <paramref name="exception"/>, <paramref name="description"/>, and <paramref name="data"/>.
        /// </summary>
        /// <param name="status">A value indicating the status of the component that was checked.</param>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).</param>
        /// <param name="data">Additional key-value pairs describing the health of the component.</param>
        public HealthCheckResult(HealthStatus status, string description = null, Exception exception = null, IReadOnlyDictionary<string, object> data = null)
        {
            Status = status;
            Description = description;
            Exception = exception;
            Data = data ?? _emptyReadOnlyDictionary;
        }

        /// <summary>
        /// Gets additional key-value pairs describing the health of the component.
        /// </summary>
        public IReadOnlyDictionary<string, object> Data { get; }

        /// <summary>
        /// Gets a human-readable description of the status of the component that was checked.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets an <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets a value indicating the status of the component that was checked.
        /// </summary>
        public HealthStatus Status { get; }

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a healthy component.
        /// </summary>
        /// <param name="description">A human-readable description of the status of the component that was checked. Optional.</param>
        /// <param name="data">Additional key-value pairs describing the health of the component. Optional.</param>
        /// <returns>A <see cref="HealthCheckResult"/> representing a healthy component.</returns>
        public static HealthCheckResult Healthy(string description = null, IReadOnlyDictionary<string, object> data = null)
        {
            return new HealthCheckResult(status: HealthStatus.Healthy, description, exception: null, data);
        }


        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a degraded component.
        /// </summary>
        /// <param name="description">A human-readable description of the status of the component that was checked. Optional.</param>
        /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status. Optional.</param>
        /// <param name="data">Additional key-value pairs describing the health of the component. Optional.</param>
        /// <returns>A <see cref="HealthCheckResult"/> representing a degraged component.</returns>
        public static HealthCheckResult Degraded(string description = null, Exception exception = null, IReadOnlyDictionary<string, object> data = null)
        {
            return new HealthCheckResult(status: HealthStatus.Degraded, description, exception: exception, data);
        }

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing an unhealthy component.
        /// </summary>
        /// <param name="description">A human-readable description of the status of the component that was checked. Optional.</param>
        /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status. Optional.</param>
        /// <param name="data">Additional key-value pairs describing the health of the component. Optional.</param>
        /// <returns>A <see cref="HealthCheckResult"/> representing an unhealthy component.</returns>
        public static HealthCheckResult Unhealthy(string description = null, Exception exception = null, IReadOnlyDictionary<string, object> data = null)
        {
            return new HealthCheckResult(status: HealthStatus.Unhealthy, description, exception, data);
        }
    }
}
