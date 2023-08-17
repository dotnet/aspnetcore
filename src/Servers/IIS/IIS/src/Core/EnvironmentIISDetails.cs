// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class EnvironmentIISDetails : IIISEnvironmentFeature
{
    public static IIISEnvironmentFeature? Create() => new EnvironmentIISDetails() is { IsAvailable: true } feature ? feature : null;

    private bool IsAvailable => IISVersion is not null;

    private EnvironmentIISDetails()
    {
        if (Version.TryParse(Get(nameof(IISVersion)), out var version))
        {
            IISVersion = version;
        }

        if (uint.TryParse(Get(nameof(SiteId)), out var siteId))
        {
            SiteId = siteId;
        }

        AppPoolName = Get(nameof(AppPoolName));
        AppPoolConfigFile = Get(nameof(AppPoolConfigFile));
        AppConfigPath = Get(nameof(AppConfigPath));
        ApplicationPhysicalPath = Get(nameof(ApplicationPhysicalPath));
        ApplicationVirtualPath = Get(nameof(ApplicationVirtualPath));
        ApplicationId = Get(nameof(ApplicationId));
        SiteName = Get(nameof(SiteName));
    }

    private static string Get([CallerMemberName] string? name = null!) => Environment.GetEnvironmentVariable($"ANCM_{name}") ?? string.Empty;

    public Version IISVersion { get; } = null!;

    public string AppPoolName { get; }

    public string AppPoolConfigFile { get; }

    public string AppConfigPath { get; }

    public string ApplicationPhysicalPath { get; }

    public string ApplicationVirtualPath { get; }

    public string ApplicationId { get; }

    public string SiteName { get; }

    public uint SiteId { get; }
}
