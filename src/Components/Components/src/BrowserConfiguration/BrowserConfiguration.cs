// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Configuration that flows from the server to the Blazor client in the browser
/// via a DOM comment. Only serializable options; callbacks stay with JS initializers.
/// </summary>
public sealed class BrowserConfiguration
{
    /// <summary>
    /// The log level for the Blazor JS runtime. Applies to all render modes.
    /// Maps to <c>WebStartOptions.logLevel</c>.
    /// </summary>
    public int? LogLevel { get; set; }

    /// <summary>WebAssembly-specific options.</summary>
    public WebAssemblyBrowserOptions WebAssembly { get; set; } = new();

    /// <summary>Server/Circuit-specific options.</summary>
    public ServerBrowserOptions Server { get; set; } = new();

    /// <summary>SSR-specific options.</summary>
    public SsrBrowserOptions Ssr { get; set; } = new();
}
