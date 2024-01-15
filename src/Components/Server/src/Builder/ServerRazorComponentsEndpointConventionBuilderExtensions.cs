// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Interactive server specific endpoint conventions for razor component applications.
/// </summary>
public static class ServerRazorComponentsEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Configures the application to support the <see cref="RenderMode.InteractiveServer"/> render mode.
    /// </summary>
    /// <returns>The <see cref="RazorComponentsEndpointConventionBuilder"/>.</returns>
    public static RazorComponentsEndpointConventionBuilder AddInteractiveServerRenderMode(this RazorComponentsEndpointConventionBuilder builder)
    {
        ComponentEndpointConventionBuilderHelper.AddRenderMode(builder, new InternalServerRenderMode());
        return builder;
    }

    /// <summary>
    ///Maps the Blazor <see cref="Hub" /> to the default path.
    /// </summary>
    /// <param name="builder">The <see cref="RazorComponentsEndpointConventionBuilder"/>.</param>
    /// <param name="configureOptions">A callback to configure dispatcher options.</param>
    /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
    public static RazorComponentsEndpointConventionBuilder AddInteractiveServerRenderMode(
        this RazorComponentsEndpointConventionBuilder builder,
        Action<HttpConnectionDispatcherOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);
        ComponentEndpointConventionBuilderHelper.AddRenderMode(builder, new InternalServerRenderMode(configureOptions));
        return builder;
    }
}
