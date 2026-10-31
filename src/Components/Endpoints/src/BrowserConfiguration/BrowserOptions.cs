// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Options that flow from the server to the Blazor client in the browser
/// via a DOM comment. Only serializable options are included; callbacks stay
/// with JS initializers.
/// </summary>
public sealed class BrowserOptions
{
    private static readonly object Key = new();

    /// <summary>
    /// Gets or sets the log level for the Blazor JS runtime. Applies to all render modes.
    /// Maps to <c>WebStartOptions.logLevel</c>.
    /// </summary>
    public LogLevel? LogLevel { get; set; }

    /// <summary>Gets the interactive WebAssembly-specific options.</summary>
    [JsonPropertyName("webAssembly")]
    public InteractiveWebAssemblyBrowserOptions InteractiveWebAssembly { get; } = new();

    /// <summary>Gets the interactive server (circuit) specific options.</summary>
    [JsonPropertyName("server")]
    public InteractiveServerBrowserOptions InteractiveServer { get; } = new();

    /// <summary>Gets the static server (SSR) specific options.</summary>
    [JsonPropertyName("ssr")]
    public StaticServerBrowserOptions StaticServer { get; } = new();

    /// <summary>
    /// Gets the <see cref="BrowserOptions"/> for the current request.
    /// If not already set, seeds from endpoint metadata or creates a new instance.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="BrowserOptions"/> for the current request.</returns>
    public static BrowserOptions GetBrowserOptions(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Items.TryGetValue(Key, out var result))
        {
            // Seed from endpoint metadata if available
            var metadataOptions = context.GetEndpoint()?.Metadata.GetMetadata<BrowserOptions>();
            var options = metadataOptions ?? new BrowserOptions();
            context.Items[Key] = options;
            return options;
        }

        return (BrowserOptions)result!;
    }
}
