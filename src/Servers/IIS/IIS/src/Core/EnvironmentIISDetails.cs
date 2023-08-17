// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class EnvironmentIISDetails : IIISEnvironmentFeature
{
    public bool IsAvailable => IISVersion is not null;

    public EnvironmentIISDetails()
    {
        if (Version.TryParse(Get("ANCM_IIS_VERSION"), out var version))
        {
            IISVersion = version;
        }

        if (uint.TryParse(Get("ANCM_SITE_ID"), out var siteId))
        {
            SiteId = siteId;
        }

        AppPoolId = Get("APP_POOL_ID");
        AppPoolConfigFile = Get("APP_POOL_CONFIG");
        AppConfigPath = Get("ANCM_APP_CONFIG_PATH");
        ApplicationPhysicalPath = Get("ANCM_APPLICATION_PHYSICAL_PATH");
        ApplicationVirtualPath = Get("ANCM_APPLICATION_VIRTUAL_PATH");
        ApplicationId = Get("ANCM_APPLICATION_VIRTUAL_PATH");
        SiteName = Get("ANCM_SITE_NAME");
    }

    private static string Get(string name) => Environment.GetEnvironmentVariable(name) ?? string.Empty;

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
