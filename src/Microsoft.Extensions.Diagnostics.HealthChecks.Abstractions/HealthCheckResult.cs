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

        private string _description;
        private IReadOnlyDictionary<string, object> _data;

        /// <summary>
        /// Gets a <see cref="HealthCheckStatus"/> value indicating the status of the component that was checked.
        /// </summary>
        public HealthCheckStatus Status { get; }

        /// <summary>
        /// Gets an <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).
        /// </summary>
        /// <remarks>
        /// This value is expected to be 'null' if <see cref="Status"/> is <see cref="HealthCheckStatus.Healthy"/>.
        /// </remarks>
        public Exception Exception { get; }

        /// <summary>
        /// Gets a human-readable description of the status of the component that was checked.
        /// </summary>
        public string Description => _description ?? string.Empty;

        /// <summary>
        /// Gets additional key-value pairs describing the health of the component.
        /// </summary>
        public IReadOnlyDictionary<string, object> Data => _data ?? _emptyReadOnlyDictionary;

        /// <summary>
        /// Creates a new <see cref="HealthCheckResult"/> with the specified <paramref name="status"/>, <paramref name="exception"/>,
        /// <paramref name="description"/>, and <paramref name="data"/>.
        /// </summary>
        /// <param name="status">A <see cref="HealthCheckStatus"/> value indicating the status of the component that was checked.</param>
        /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).</param>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        /// <param name="data">Additional key-value pairs describing the health of the component.</param>
        public HealthCheckResult(HealthCheckStatus status, Exception exception, string description, IReadOnlyDictionary<string, object> data)
        {
            if (status == HealthCheckStatus.Unknown)
            {
                throw new ArgumentException($"'{nameof(HealthCheckStatus.Unknown)}' is not a valid value for the 'status' parameter.", nameof(status));
            }

            Status = status;
            Exception = exception;
            _description = description;
            _data = data;
        }

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing an unhealthy component.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing an unhealthy component.</returns>
        public static HealthCheckResult Unhealthy()
            => new HealthCheckResult(HealthCheckStatus.Unhealthy, exception: null, description: string.Empty, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing an unhealthy component.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing an unhealthy component.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        public static HealthCheckResult Unhealthy(string description)
            => new HealthCheckResult(HealthCheckStatus.Unhealthy, exception: null, description: description, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing an unhealthy component.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing an unhealthy component.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        /// <param name="data">Additional key-value pairs describing the health of the component.</param>
        public static HealthCheckResult Unhealthy(string description, IReadOnlyDictionary<string, object> data)
            => new HealthCheckResult(HealthCheckStatus.Unhealthy, exception: null, description: description, data: data);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing an unhealthy component.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing an unhealthy component.</returns>
        /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).</param>
        public static HealthCheckResult Unhealthy(Exception exception)
            => new HealthCheckResult(HealthCheckStatus.Unhealthy, exception, description: string.Empty, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing an unhealthy component.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing an unhealthy component.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).</param>
        public static HealthCheckResult Unhealthy(string description, Exception exception)
            => new HealthCheckResult(HealthCheckStatus.Unhealthy, exception, description, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing an unhealthy component.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing an unhealthy component.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).</param>
        /// <param name="data">Additional key-value pairs describing the health of the component.</param>
        public static HealthCheckResult Unhealthy(string description, Exception exception, IReadOnlyDictionary<string, object> data)
            => new HealthCheckResult(HealthCheckStatus.Unhealthy, exception, description, data);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a healthy component.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing a healthy component.</returns>
        public static HealthCheckResult Healthy()
            => new HealthCheckResult(HealthCheckStatus.Healthy, exception: null, description: string.Empty, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a healthy component.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing a healthy component.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        public static HealthCheckResult Healthy(string description)
            => new HealthCheckResult(HealthCheckStatus.Healthy, exception: null, description: description, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a healthy component.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing a healthy component.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        /// <param name="data">Additional key-value pairs describing the health of the component.</param>
        public static HealthCheckResult Healthy(string description, IReadOnlyDictionary<string, object> data)
            => new HealthCheckResult(HealthCheckStatus.Healthy, exception: null, description: description, data: data);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a component in a degraded state.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing a component in a degraded state.</returns>
        public static HealthCheckResult Degraded()
            => new HealthCheckResult(HealthCheckStatus.Degraded, exception: null, description: string.Empty, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a component in a degraded state.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing a component in a degraded state.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        public static HealthCheckResult Degraded(string description)
            => new HealthCheckResult(HealthCheckStatus.Degraded, exception: null, description: description, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a component in a degraded state.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing a component in a degraded state.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        /// <param name="data">Additional key-value pairs describing the health of the component.</param>
        public static HealthCheckResult Degraded(string description, IReadOnlyDictionary<string, object> data)
            => new HealthCheckResult(HealthCheckStatus.Degraded, exception: null, description: description, data: data);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a component in a degraded state.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing a component in a degraded state.</returns>
        public static HealthCheckResult Degraded(Exception exception)
            => new HealthCheckResult(HealthCheckStatus.Degraded, exception: null, description: string.Empty, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a component in a degraded state.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing a component in a degraded state.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).</param>
        public static HealthCheckResult Degraded(string description, Exception exception)
            => new HealthCheckResult(HealthCheckStatus.Degraded, exception, description, data: null);

        /// <summary>
        /// Creates a <see cref="HealthCheckResult"/> representing a component in a degraded state.
        /// </summary>
        /// <returns>A <see cref="HealthCheckResult"/> representing a component in a degraded state.</returns>
        /// <param name="description">A human-readable description of the status of the component that was checked.</param>
        /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).</param>
        /// <param name="data">Additional key-value pairs describing the health of the component.</param>
        public static HealthCheckResult Degraded(string description, Exception exception, IReadOnlyDictionary<string, object> data)
            => new HealthCheckResult(HealthCheckStatus.Degraded, exception, description, data);
    }
}
