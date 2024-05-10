// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.StaticAssets.Infrastructure;

/// <summary>
/// This type is not recommended for use outside of ASP.NET Core.
/// </summary>
public static class StaticAssetsEndpointDataSourceHelper
{
    /// <summary>
    /// This method is not recommended for use outside of ASP.NET Core.
    /// </summary>
    /// <param name="dataSource"></param>
    /// <param name="staticAssetsManifestPath"></param>

    public static bool IsStaticAssetsDataSource(EndpointDataSource dataSource, string? staticAssetsManifestPath = null)
    {
        if (dataSource is StaticAssetsEndpointDataSource staticAssetsDataSource)
        {
            if (staticAssetsManifestPath is null)
            {
                var serviceProvider = staticAssetsDataSource.ServiceProvider;
                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                staticAssetsManifestPath = Path.Combine(AppContext.BaseDirectory, $"{environment.ApplicationName}.staticwebassets.endpoints.json");
            }

            staticAssetsManifestPath = Path.IsPathRooted(staticAssetsManifestPath) ? staticAssetsManifestPath : Path.Combine(AppContext.BaseDirectory, staticAssetsManifestPath);

            return string.Equals(staticAssetsDataSource.ManifestPath, staticAssetsManifestPath, StringComparison.Ordinal);
        }

        return false;
    }
}
