// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticAssets.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Web assembly specific endpoint conventions for razor component applications.
/// </summary>
public static partial class WebAssemblyRazorComponentsEndpointConventionBuilderExtensions
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
        if (HasStaticAssetDataSource(endpointBuilder, options.StaticAssetsManifestPath))
        {
            options.ConventionsApplied = true;
            endpointBuilder.MapStaticAssets(options.StaticAssetsManifestPath)
                .AddBlazorWebAssemblyConventions();

            return builder;
        }
        else if (environment.IsDevelopment())
        {
            var logger = endpointBuilder.ServiceProvider.GetRequiredService<ILogger<WebAssemblyComponentsEndpointOptions>>();
            if (options.StaticAssetsManifestPath is null)
            {
                Log.StaticAssetsMappingNotFoundForDefaultManifest(logger);
            }
            else
            {
                Log.StaticAssetsMappingNotFoundWithManifest(logger, options.StaticAssetsManifestPath);
            }
        }

        return builder;
    }

    private static bool HasStaticAssetDataSource(IEndpointRouteBuilder endpointRouteBuilder, string? staticAssetsManifestName)
    {
        foreach (var ds in endpointRouteBuilder.DataSources)
        {
            if (StaticAssetsEndpointDataSourceHelper.IsStaticAssetsDataSource(ds, staticAssetsManifestName))
            {
                return true;
            }
        }

        return false;
    }

    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, $$"""Mapped static asset endpoints not found. Ensure '{{nameof(StaticAssetsEndpointRouteBuilderExtensions.MapStaticAssets)}}' is called before '{{nameof(AddInteractiveWebAssemblyRenderMode)}}'.""")]
        internal static partial void StaticAssetsMappingNotFoundForDefaultManifest(ILogger logger);

        [LoggerMessage(2, LogLevel.Warning, $$"""Mapped static asset endpoints not found for manifest '{ManifestPath}'. Ensure '{{nameof(StaticAssetsEndpointRouteBuilderExtensions.MapStaticAssets)}}'(staticAssetsManifestPath) is called before '{{nameof(AddInteractiveWebAssemblyRenderMode)}}' and that both manifest paths are the same.""")]
        internal static partial void StaticAssetsMappingNotFoundWithManifest(ILogger logger, string manifestPath);
    }
}
