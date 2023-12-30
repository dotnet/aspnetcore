// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class IISEnvironmentFeature : IIISEnvironmentFeature
{
    public static bool TryCreate(IConfiguration configuration, [NotNullWhen(true)] out IIISEnvironmentFeature? result)
    {
        var feature = new IISEnvironmentFeature(configuration);

        if (feature.IISVersion is not null)
        {
            result = feature;
            return true;
        }

        result = null;
        return false;
    }

    private IISEnvironmentFeature(IConfiguration configuration)
    {
        if (Version.TryParse(configuration["IIS_VERSION"], out var version))
        {
            IISVersion = version;
        }

        if (uint.TryParse(configuration["IIS_SITE_ID"], out var siteId))
        {
            SiteId = siteId;
        }

        AppPoolId = configuration["IIS_APP_POOL_ID"] ?? string.Empty;
        AppPoolConfigFile = configuration["IIS_APP_POOL_CONFIG_FILE"] ?? string.Empty;
        AppConfigPath = configuration["IIS_APP_CONFIG_PATH"] ?? string.Empty;
        ApplicationPhysicalPath = configuration["IIS_PHYSICAL_PATH"] ?? string.Empty;
        ApplicationVirtualPath = configuration["IIS_APPLICATION_VIRTUAL_PATH"] ?? string.Empty;
        ApplicationId = configuration["IIS_APPLICATION_ID"] ?? string.Empty;
        SiteName = configuration["IIS_SITE_NAME"] ?? string.Empty;
    }

    public Version IISVersion { get; } = null!;

    public string AppPoolId { get; }

    public string AppPoolConfigFile { get; }

    public string AppConfigPath { get; }

    public string ApplicationPhysicalPath { get; }

    public string ApplicationVirtualPath { get; }

    public string ApplicationId { get; }

    public string SiteName { get; }

    public uint SiteId { get; }
}
