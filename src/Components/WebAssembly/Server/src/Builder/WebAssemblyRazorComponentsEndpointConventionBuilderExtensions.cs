// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Server;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Web assembly specific endpoint conventions for razor component applications.
/// </summary>
public static class WebAssemblyRazorComponentsEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="RenderMode.WebAssembly"/> for this application.
    /// </summary>
    /// <returns>The <see cref="RazorComponentsEndpointConventionBuilder"/>.</returns>
    public static RazorComponentsEndpointConventionBuilder AddWebAssemblyRenderMode(
        this RazorComponentsEndpointConventionBuilder builder,
        WebAssemblyComponentsEndpointOptions? options = null)
    {
        builder.AddRenderMode(new WebAssemblyRenderModeWithOptions(options));
        return builder;
    }
}
