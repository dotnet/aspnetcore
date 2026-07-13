// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Options that flow from the server to the Blazor client in the browser
/// via a DOM comment. Only serializable options are included; callbacks stay
/// with JS initializers.
/// </summary>
public sealed class BrowserOptions
{
    /// <summary>
    /// Gets or sets the log level for the Blazor JS runtime. Applies to all render modes.
    /// Maps to <c>WebStartOptions.logLevel</c>.
    /// </summary>
    public LogLevel? LogLevel { get; set; }

    /// <summary>Gets the WebAssembly-specific options.</summary>
    public WebAssemblyBrowserOptions WebAssembly { get; } = new();

    /// <summary>Gets the interactive server (circuit) specific options.</summary>
    public InteractiveServerBrowserOptions Server { get; } = new();

    /// <summary>Gets the SSR-specific options.</summary>
    public SsrBrowserOptions Ssr { get; } = new();
}
