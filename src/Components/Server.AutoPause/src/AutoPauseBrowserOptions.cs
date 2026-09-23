// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Configures the opt-in auto-pause behavior for a Blazor Server circuit. When enabled,
/// the circuit is automatically paused after the browser tab has been hidden for
/// <see cref="HiddenDelay"/>, and resumed when the tab becomes visible again.
/// </summary>
/// <example>
/// <code>
/// app.MapRazorComponents&lt;App&gt;()
///    .WithBrowserOptions(options => options.AddAutoPause(p => p.HiddenDelay = TimeSpan.FromSeconds(30)));
/// </code>
/// </example>
public sealed class AutoPauseBrowserOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether auto-pause is enabled.
    /// </summary>
    /// <value>Defaults to <c>true</c> (enabling it is the act of calling <c>AddAutoPause</c>).</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the delay after the tab becomes hidden before the circuit is paused.
    /// </summary>
    /// <value>Defaults to two minutes. Must be greater than <see cref="TimeSpan.Zero"/>.</value>
    public TimeSpan HiddenDelay { get; set; } = TimeSpan.FromMinutes(2);
}
