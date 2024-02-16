// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using System.Linq;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Web assembly specific endpoint conventions for razor component applications.
/// </summary>
public static class WebAssemblyRazorComponentsEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Configures the application to support the <see cref="RenderMode.InteractiveWebAssembly"/> render mode.
    /// </summary>
    /// <returns>The <see cref="RazorComponentsEndpointConventionBuilder"/>.</returns>
    public static RazorComponentsEndpointConventionBuilder AddInteractiveWebAssemblyRenderMode(
        this RazorComponentsEndpointConventionBuilder builder,
        Action<WebAssemblyComponentsEndpointOptions>? callback = null)
    {
        var options = new WebAssemblyComponentsEndpointOptions();

        callback?.Invoke(options);

        if (options.ServeMultithreadingHeaders)
        {
            builder.Add(endpointBuilder =>
            {
                var needsCoopHeaders = endpointBuilder.Metadata.OfType<ComponentTypeMetadata>().Any() // e.g., /somecomponent
                    || endpointBuilder.Metadata.OfType<WebAssemblyRenderModeWithOptions>().Any();     // e.g., /_framework/*
                if (needsCoopHeaders && endpointBuilder.RequestDelegate is { } originalDelegate)
                {
                    endpointBuilder.RequestDelegate = httpContext =>
                    {
                        httpContext.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
                        httpContext.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                        return originalDelegate(httpContext);
                    };
                }
            });
        }

        ComponentEndpointConventionBuilderHelper.AddRenderMode(builder, new WebAssemblyRenderModeWithOptions(options));
        return builder;
    }
}
