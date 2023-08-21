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
}
