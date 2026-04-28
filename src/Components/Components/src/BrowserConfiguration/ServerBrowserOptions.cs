// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Serializable subset of <c>CircuitStartOptions</c>.
/// Non-serializable options (<c>configureSignalR</c>, <c>reconnectionHandler</c>,
/// <c>circuitHandlers</c>) must use <c>Blazor.start()</c> or JS initializers.
/// </summary>
public sealed class ServerBrowserOptions
{
    /// <summary>
    /// Maximum reconnection attempts before giving up.
    /// Maps to <c>CircuitStartOptions.reconnectionOptions.maxRetries</c>.
    /// </summary>
    public int? ReconnectionMaxRetries { get; set; }

    /// <summary>
    /// Base interval in milliseconds between reconnection attempts (scalar form).
    /// The function form <c>(retryCount, currentMs) => number</c> requires JS.
    /// Maps to <c>CircuitStartOptions.reconnectionOptions.retryIntervalMilliseconds</c>.
    /// </summary>
    public int? ReconnectionRetryIntervalMilliseconds { get; set; }

    /// <summary>
    /// CSS ID of the reconnection dialog element.
    /// Maps to <c>CircuitStartOptions.reconnectionOptions.dialogId</c>.
    /// </summary>
    public string? ReconnectionDialogId { get; set; }
}
