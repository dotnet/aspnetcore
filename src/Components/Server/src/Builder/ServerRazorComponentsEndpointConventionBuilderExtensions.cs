// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Interactive server specific endpoint conventions for razor component applications.
/// </summary>
public static class ServerRazorComponentsEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="RenderMode.Server"/> for this application.
    /// </summary>
    /// <returns>The <see cref="RazorComponentsEndpointConventionBuilder"/>.</returns>
    public static RazorComponentsEndpointConventionBuilder AddServerRenderMode(this RazorComponentsEndpointConventionBuilder builder)
    {
        builder.AddRenderMode(RenderMode.Server);

        return builder;
    }
}
