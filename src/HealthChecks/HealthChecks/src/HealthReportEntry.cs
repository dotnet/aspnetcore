// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Represents an entry in a <see cref="HealthReport"/>. Corresponds to the result of a single <see cref="IHealthCheck"/>.
/// </summary>
public struct HealthReportEntry
{
    private static readonly IReadOnlyDictionary<string, object> _emptyReadOnlyDictionary = new Dictionary<string, object>();

    /// <summary>
    /// Creates a new <see cref="HealthReportEntry"/> with the specified values for <paramref name="status"/>, <paramref name="exception"/>,
    /// <paramref name="description"/>, and <paramref name="data"/>.
    /// </summary>
    /// <param name="status">A value indicating the health status of the component that was checked.</param>
    /// <param name="description">A human-readable description of the status of the component that was checked.</param>
    /// <param name="duration">A value indicating the health execution duration.</param>
    /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).</param>
    /// <param name="data">Additional key-value pairs describing the health of the component.</param>
    public HealthReportEntry(HealthStatus status, string? description, TimeSpan duration, Exception? exception, IReadOnlyDictionary<string, object>? data)
        : this(status, description, duration, exception, data, null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="HealthReportEntry"/> with the specified values for <paramref name="status"/>, <paramref name="exception"/>,
    /// <paramref name="description"/>, and <paramref name="data"/>.
    /// </summary>
    /// <param name="status">A value indicating the health status of the component that was checked.</param>
    /// <param name="description">A human-readable description of the status of the component that was checked.</param>
    /// <param name="duration">A value indicating the health execution duration.</param>
    /// <param name="exception">An <see cref="Exception"/> representing the exception that was thrown when checking for status (if any).</param>
    /// <param name="data">Additional key-value pairs describing the health of the component.</param>
    /// <param name="tags">Tags associated with the health check that generated the report entry.</param>
    public HealthReportEntry(HealthStatus status, string? description, TimeSpan duration, Exception? exception, IReadOnlyDictionary<string, object>? data, IEnumerable<string>? tags = null)
    {
        Status = status;
        Description = description;
        Duration = duration;
        Exception = exception;
        Data = data ?? _emptyReadOnlyDictionary;
        Tags = tags ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Gets additional key-value pairs describing the health of the component.
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// Gets a human-readable description of the status of the component that was checked.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the health check execution duration.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets an <see cref="System.Exception"/> representing the exception that was thrown when checking for status (if any).
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the health status of the component that was checked.
    /// </summary>
    public HealthStatus Status { get; }

    /// <summary>
    /// Gets the tags associated with the health check.
    /// </summary>
    public IEnumerable<string> Tags { get; }
}
