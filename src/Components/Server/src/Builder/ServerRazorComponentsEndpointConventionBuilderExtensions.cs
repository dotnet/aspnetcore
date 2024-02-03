// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
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
        return AddInteractiveServerRenderMode(builder, null);
    }

    /// <summary>
    /// Maps the Blazor <see cref="Hub" /> to the default path.
    /// </summary>
    /// <param name="builder">The <see cref="RazorComponentsEndpointConventionBuilder"/>.</param>
    /// <param name="callback">A callback to configure server endpoint options.</param>
    /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
    public static RazorComponentsEndpointConventionBuilder AddInteractiveServerRenderMode(
        this RazorComponentsEndpointConventionBuilder builder,
        Action<ServerComponentsEndpointOptions>? callback = null)
    {
        var options = new ServerComponentsEndpointOptions();
        callback?.Invoke(options);

        ComponentEndpointConventionBuilderHelper.AddRenderMode(builder, new InternalServerRenderMode(options));

        if (options.ConfigureWebsocketOptions is not null && options.ContentSecurityFrameAncestorPolicy != null)
        {
            builder.Add(b =>
            {
                for (var i = 0; i < b.Metadata.Count; i++)
                {
                    var metadata = b.Metadata[i];
                    if (metadata is ComponentTypeMetadata)
                    {
                        var original = b.RequestDelegate;
                        b.RequestDelegate = async context =>
                        {
                            context.Response.Headers.Add("Content-Security-Policy", $"frame-ancestors {options.ContentSecurityFrameAncestorPolicy}");
                            await original(context);
                        };
                    }
                }
            });
        }

        return builder;
    }
}
