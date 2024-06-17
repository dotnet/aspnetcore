// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

/// <summary>
/// Options to configure interactive WebAssembly components.
/// </summary>
public sealed class WebAssemblyComponentsEndpointOptions
{
    /// <summary>
    /// Gets or sets the <see cref="PathString"/> that indicates the prefix for Blazor WebAssembly assets.
    /// This path must correspond to a referenced Blazor WebAssembly application project.
    /// </summary>
    public PathString PathPrefix { get; set; }

    /// <summary>
    /// Gets or sets a flag to determine whether to enable WebAssembly multithreading. If true,
    /// the server will add headers similar to <code>Cross-Origin-Embedder-Policy: require-corp</code> and
    /// <code>Cross-Origin-Opener-Policy: same-origin</code> on the response for the host page, because
    /// this is required to enable the SharedArrayBuffer feature in the browser.
    ///
    /// Note that enabling this feature can restrict your ability to use other JavaScript APIs. For more
    /// information, see <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/SharedArrayBuffer#security_requirements" />.
    /// </summary>
    public bool ServeMultithreadingHeaders { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="string"/> that determines the static assets manifest path mapped to this app.
    /// </summary>
    public string? StaticAssetsManifestPath { get; set; }
}
