// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Serializable auto-pause options that flow from the server to the browser.
/// Non-serializable options (<c>onPauseRequested</c>) must use <c>Blazor.start()</c>
/// or JS initializers.
/// Maps to <c>CircuitStartOptions.autoPause</c>.
/// </summary>
public sealed class AutoPauseBrowserOptions
{
    /// <summary>
    /// Gets or sets whether auto-pause is enabled.
    /// When <c>true</c>, the circuit will automatically pause after the tab is hidden
    /// for <see cref="HiddenDelayMilliseconds"/>.
    /// </summary>
    /// <value>Defaults to <c>false</c>.</value>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the delay in milliseconds after the tab becomes hidden before
    /// the circuit is automatically paused.
    /// </summary>
    /// <value>Defaults to <c>120000</c> (2 minutes) on the client when not set.</value>
    public int? HiddenDelayMilliseconds { get; set; }
}
