// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Web assembly specific endpoint conventions for razor component applications.
/// </summary>
public static class WebAssemblyRazorComponentEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="RenderMode.WebAssembly"/> for this application.
    /// </summary>
    /// <returns>The <see cref="RazorComponentEndpointConventionBuilder"/>.</returns>
    public static RazorComponentEndpointConventionBuilder AddWebAssemblyRenderMode(
        this RazorComponentEndpointConventionBuilder builder,
        WebAssemblyComponentsEndpointOptions? options = null)
    {
        builder.AddRenderMode(new WebAssemblyRenderModeWithOptions(options));
        return builder;
    }

}
