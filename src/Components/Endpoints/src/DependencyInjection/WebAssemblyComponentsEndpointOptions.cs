// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Options to configure interactive WebAssembly components in a razor components application.
/// </summary>
public sealed class WebAssemblyComponentsEndpointOptions
{
    /// <summary>
    /// Gets or sets <see cref="PathString"/> that indicates the prefix for Blazor WebAssembly framework files.
    /// </summary>
    public PathString FrameworkFilesPathPrefix { get; set; }
}
