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
    /// Auto-pause options for server circuits.
    /// Maps to <c>CircuitStartOptions.autoPause</c>.
    /// </summary>
    public AutoPauseBrowserOptions AutoPause { get; set; } = new();
}
