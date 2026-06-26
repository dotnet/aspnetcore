// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Serializable subset of <c>SsrStartOptions</c>.
/// </summary>
public sealed class SsrBrowserOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the DOM is preserved during enhanced navigation.
    /// When <see langword="false"/>, DOM preservation is disabled. <see langword="null"/> leaves the value unset.
    /// Maps to <c>SsrStartOptions.disableDomPreservation</c>.
    /// </summary>
    [JsonPropertyName("disableDomPreservation")]
    [JsonConverter(typeof(NegatedBooleanJsonConverter))]
    public bool? PreserveDom { get; set; }

    /// <summary>
    /// Gets or sets the timeout before an inactive circuit is disposed.
    /// Maps to <c>SsrStartOptions.circuitInactivityTimeoutMs</c>.
    /// </summary>
    [JsonPropertyName("circuitInactivityTimeoutMs")]
    [JsonConverter(typeof(TimeSpanMillisecondsJsonConverter))]
    public TimeSpan? CircuitInactivityTimeout { get; set; }
}
