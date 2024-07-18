// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Contains methods to integrate static assets with endpoints.
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
    public static StaticAssetsEndpointConventionBuilder MapStaticAssets(this IEndpointRouteBuilder endpoints, string? staticAssetsManifestPath = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var environment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        staticAssetsManifestPath ??= $"{environment.ApplicationName}.staticwebassets.endpoints.json";

        staticAssetsManifestPath = !Path.IsPathRooted(staticAssetsManifestPath) ?
            Path.Combine(AppContext.BaseDirectory, staticAssetsManifestPath) :
            staticAssetsManifestPath;

        var result = MapStaticAssetsCore(endpoints, staticAssetsManifestPath);

        if (StaticAssetDevelopmentRuntimeHandler.IsEnabled(endpoints.ServiceProvider, environment))
        {
            StaticAssetDevelopmentRuntimeHandler.EnableSupport(endpoints, result, environment, result.Descriptors);
        }

        return result;
    }

    private static StaticAssetsEndpointConventionBuilder MapStaticAssetsCore(
        IEndpointRouteBuilder endpoints,
        string manifestPath)
    {
        var builder = GetExistingBuilder(endpoints, manifestPath);
        if (builder != null)
        {
            return builder;
        }

        var manifest = ResolveManifest(manifestPath);

        var dataSource = StaticAssetsManifest.CreateDataSource(endpoints, manifestPath, manifest.Endpoints);
        return dataSource.DefaultBuilder;
    }

    private static StaticAssetsManifest ResolveManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            throw new InvalidOperationException($"The static resources manifest file '{manifestPath}' was not found.");
        }

        return StaticAssetsManifest.Parse(manifestPath);
    }

    private static StaticAssetsEndpointConventionBuilder? GetExistingBuilder(IEndpointRouteBuilder endpoints, string manifestPath)
    {
        foreach (var ds in endpoints.DataSources)
        {
            if (ds is StaticAssetsEndpointDataSource sads && sads.ManifestPath.Equals(manifestPath, StringComparison.Ordinal))
            {
                return sads.DefaultBuilder;
            }
        }

        return null;
    }

    // For testing purposes
    internal static StaticAssetsEndpointConventionBuilder MapStaticAssets(this IEndpointRouteBuilder endpoints, StaticAssetsManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var environment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var result = StaticAssetsManifest.CreateDataSource(endpoints, "", manifest.Endpoints).DefaultBuilder;

        if (StaticAssetDevelopmentRuntimeHandler.IsEnabled(endpoints.ServiceProvider, environment))
        {
            StaticAssetDevelopmentRuntimeHandler.EnableSupport(endpoints, result, environment, result.Descriptors);
        }

        return result;
    }
}
