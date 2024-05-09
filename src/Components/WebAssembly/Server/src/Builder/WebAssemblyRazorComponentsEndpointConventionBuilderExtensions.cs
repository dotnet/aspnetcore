// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        var endpointBuilder = ComponentEndpointConventionBuilderHelper.GetEndpointRouteBuilder(builder);
        var environment = endpointBuilder.ServiceProvider.GetRequiredService<IHostEnvironment>();

        // If the static assets data source for the given manifest name is already added, then just wire-up the Blazor WebAssembly conventions.
        // MapStaticWebAssetEndpoints is idempotent and will not add the data source if it already exists.
        var staticAssetsManifestPath = options.AssetsManifestPath ?? Path.Combine(AppContext.BaseDirectory, $"{environment.ApplicationName}.staticwebassets.endpoints.json");
        staticAssetsManifestPath = Path.IsPathRooted(staticAssetsManifestPath) ? staticAssetsManifestPath : Path.Combine(AppContext.BaseDirectory, staticAssetsManifestPath);
        if (HasStaticAssetDataSource(endpointBuilder, staticAssetsManifestPath))
        {
            options.ConventionsApplied = true;
            endpointBuilder.MapStaticAssetEndpoints(staticAssetsManifestPath)
                .AddBlazorWebAssemblyConventions();

            return builder;
        }

        return builder;
    }

    private static bool HasStaticAssetDataSource(IEndpointRouteBuilder endpointRouteBuilder, string? staticAssetsManifestName)
    {
        foreach (var ds in endpointRouteBuilder.DataSources)
        {
            if (ds is StaticAssetsEndpointDataSource staticAssetsDataSource &&
                string.Equals(Path.GetFileName(staticAssetsDataSource.ManifestName), staticAssetsManifestName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
