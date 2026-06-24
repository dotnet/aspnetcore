// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

/// <summary>
/// Provides options for configuring interactive WebAssembly components.
/// </summary>
public class WebAssemblyComponentsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the server's culture should be
    /// applied on the WebAssembly client. Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseCultureFromServer { get; set; } = true;
}
