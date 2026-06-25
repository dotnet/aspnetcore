// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Serializable subset of <c>CircuitStartOptions</c> for the interactive server render mode.
/// Non-serializable options (<c>configureSignalR</c>, <c>reconnectionHandler</c>,
/// <c>circuitHandlers</c>) must use <c>Blazor.start()</c> or JS initializers.
/// </summary>
public sealed class InteractiveServerBrowserOptions
{
    /// <summary>
    /// Gets or sets the maximum reconnection attempts before giving up.
    /// Maps to <c>CircuitStartOptions.reconnectionOptions.maxRetries</c>.
    /// </summary>
    public int? ReconnectionMaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the base interval between reconnection attempts (scalar form).
    /// The function form <c>(retryCount, currentMs) => number</c> requires JS.
    /// Maps to <c>CircuitStartOptions.reconnectionOptions.retryIntervalMilliseconds</c>.
    /// </summary>
    [JsonPropertyName("reconnectionRetryIntervalMilliseconds")]
    [JsonConverter(typeof(TimeSpanMillisecondsJsonConverter))]
    public TimeSpan? ReconnectionRetryInterval { get; set; }

    /// <summary>
    /// Gets or sets the CSS ID of the reconnection dialog element.
    /// Maps to <c>CircuitStartOptions.reconnectionOptions.dialogId</c>.
    /// </summary>
    public string? ReconnectionDialogId { get; set; }

    /// <summary>
    /// Gets or sets whether auto-pause is enabled.
    /// When <c>true</c>, the circuit will automatically pause after the tab is hidden
    /// for <see cref="AutoPauseHiddenDelayMilliseconds"/>.
    /// Maps to <c>CircuitStartOptions.autoPause.enabled</c>.
    /// </summary>
    /// <value>Defaults to <c>false</c>.</value>
    public bool? AutoPauseEnabled { get; set; }

    /// <summary>
    /// Gets or sets the delay in milliseconds after the tab becomes hidden before
    /// the circuit is automatically paused.
    /// Maps to <c>CircuitStartOptions.autoPause.hiddenDelayMilliseconds</c>.
    /// </summary>
    /// <value>Defaults to <c>120000</c> (2 minutes) on the client when not set.</value>
    public int? AutoPauseHiddenDelayMilliseconds { get; set; }
}
