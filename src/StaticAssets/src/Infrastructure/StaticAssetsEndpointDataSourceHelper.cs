// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.StaticAssets.Infrastructure;

/// <summary>
/// For internal framework use only.
/// </summary>
public static class StaticAssetsEndpointDataSourceHelper
{
    /// <summary>
    /// For internal framework use only.
    /// </summary>
    public static bool HasStaticAssetsDataSource(IEndpointRouteBuilder builder, string? staticAssetsManifestPath = null)
    {
        staticAssetsManifestPath = ApplyStaticAssetManifestPathConventions(staticAssetsManifestPath, builder.ServiceProvider);
        foreach (var dataSource in builder.DataSources)
        {
            if (dataSource is StaticAssetsEndpointDataSource staticAssetsDataSource)
            {
                if (string.Equals(staticAssetsDataSource.ManifestPath, staticAssetsManifestPath, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// For internal framework use only.
    /// </summary>
    public static IReadOnlyList<StaticAssetDescriptor> ResolveStaticAssetDescriptors(
        IEndpointRouteBuilder endpointRouteBuilder,
        string? manifestPath)
    {
        manifestPath = ApplyStaticAssetManifestPathConventions(manifestPath, endpointRouteBuilder.ServiceProvider);
        foreach (var dataSource in endpointRouteBuilder.DataSources)
        {
            if (dataSource is StaticAssetsEndpointDataSource staticAssetsDataSource &&
                string.Equals(staticAssetsDataSource.ManifestPath, manifestPath, StringComparison.Ordinal))
            {
                return staticAssetsDataSource.Descriptors;
            }
        }

        return [];
    }

    internal static string ApplyStaticAssetManifestPathConventions(string? staticAssetsManifestPath, IServiceProvider services)
    {
        if (staticAssetsManifestPath is null)
        {
            var environment = services.GetRequiredService<IWebHostEnvironment>();
            return Path.Combine(AppContext.BaseDirectory, $"{environment.ApplicationName}.staticwebassets.endpoints.json");
        }

        return Path.IsPathRooted(staticAssetsManifestPath) ? staticAssetsManifestPath : Path.Combine(AppContext.BaseDirectory, staticAssetsManifestPath);
    }
}
