// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Contains methods to integrate static assets with endpoints
/// </summary>
public static class StaticAssetsEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps static files produced during the build as endpoints.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="staticAssetsManifestPath">The path to the static assets manifest file.</param>
    /// <remarks>
    /// The <paramref name="staticAssetsManifestPath"/> can be null to use the <see cref="IHostEnvironment.ApplicationName"/> to locate the manifes.
    /// As an alternative, a full path can be specified to the manifest file. If a relative path is used, we'll search for the file in the <see cref="AppContext.BaseDirectory"/>." />
    /// </remarks>
    public static StaticAssetsEndpointConventionBuilder MapStaticAssetEndpoints(this IEndpointRouteBuilder endpoints, string? staticAssetsManifestPath = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var environment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        staticAssetsManifestPath ??= $"{environment.ApplicationName}.staticwebassets.endpoints.json";

        staticAssetsManifestPath = !Path.IsPathRooted(staticAssetsManifestPath) ?
            Path.Combine(AppContext.BaseDirectory, staticAssetsManifestPath) :
            staticAssetsManifestPath;

        var result = MapStaticAssetEndpointsCore(endpoints, staticAssetsManifestPath);

        if (StaticAssetDevelopmentRuntimeHandler.IsEnabled(endpoints.ServiceProvider, environment))
        {
            StaticAssetDevelopmentRuntimeHandler.EnableSupport(endpoints, result, environment, result.Descriptors);
        }

        return result;
    }

    private static StaticAssetsEndpointConventionBuilder MapStaticAssetEndpointsCore(
        IEndpointRouteBuilder endpoints,
        string manifestPath,
        StaticAssetsManifest? manifest = null)
    {
        foreach (var ds in endpoints.DataSources)
        {
            if (ds is StaticAssetsEndpointDataSource sads && sads.ManifestName.Equals(manifestPath, StringComparison.Ordinal))
            {
                return sads.DefaultBuilder;
            }
        }

        if (manifest == null && !File.Exists(manifestPath))
        {
            throw new InvalidOperationException($"The static resources manifest file '{manifestPath}' was not found.");
        }

        manifest ??= StaticAssetsManifest.Parse(manifestPath);

        var dataSource = manifest.CreateDataSource(endpoints, manifestPath, manifest.Endpoints);
        return dataSource.DefaultBuilder;
    }

    // For testing purposes
    internal static StaticAssetsEndpointConventionBuilder MapStaticAssetEndpoints(this IEndpointRouteBuilder endpoints, StaticAssetsManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var environment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var result = MapStaticAssetEndpointsCore(endpoints, "unused", manifest);

        if (StaticAssetDevelopmentRuntimeHandler.IsEnabled(endpoints.ServiceProvider, environment))
        {
            StaticAssetDevelopmentRuntimeHandler.EnableSupport(endpoints, result, environment, result.Descriptors);
        }

        return result;
    }

}
