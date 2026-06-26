// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Extension methods for enabling auto-pause through <see cref="BrowserOptions"/>.
/// </summary>
public static class AutoPauseBrowserOptionsExtensions
{
    /// <summary>
    /// Enables automatic circuit pausing for hidden tabs. The configuration is written to the
    /// circuit's library extension data and flows to the client, where the auto-pause JavaScript
    /// initializer reads it. No core Blazor type needs to know about auto-pause.
    /// </summary>
    /// <param name="options">The <see cref="BrowserOptions"/> to configure.</param>
    /// <param name="configure">An optional callback to customize the auto-pause behavior.</param>
    /// <returns>The same <see cref="BrowserOptions"/> instance for chaining.</returns>
    public static BrowserOptions AddAutoPause(this BrowserOptions options, Action<AutoPauseBrowserOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var settings = new AutoPauseBrowserOptions();
        configure?.Invoke(settings);

        if (settings.HiddenDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(configure), settings.HiddenDelay, $"{nameof(AutoPauseBrowserOptions.HiddenDelay)} must be greater than zero.");
        }

        // Flat keys follow the existing browser-config convention (see InteractiveServerBrowserOptions).
        options.Server.Extensions["autoPauseEnabled"] = JsonSerializer.SerializeToElement(settings.Enabled, AutoPauseJsonContext.Default.Boolean);
        options.Server.Extensions["autoPauseHiddenDelayMilliseconds"] = JsonSerializer.SerializeToElement((int)settings.HiddenDelay.TotalMilliseconds, AutoPauseJsonContext.Default.Int32);

        return options;
    }
}

[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
internal sealed partial class AutoPauseJsonContext : JsonSerializerContext
{
}
