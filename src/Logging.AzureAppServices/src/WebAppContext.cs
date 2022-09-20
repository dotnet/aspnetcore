// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.AzureAppServices;

/// <summary>
/// Represents the default implementation of <see cref="IWebAppContext"/>.
/// </summary>
internal sealed class WebAppContext : IWebAppContext
{
    /// <summary>
    /// Gets the default instance of the WebApp context.
    /// </summary>
    public static WebAppContext Default { get; } = new WebAppContext();

    private WebAppContext() { }

    /// <inheritdoc />
    public string HomeFolder { get; } = Environment.GetEnvironmentVariable("HOME");

    /// <inheritdoc />
    public string SiteName { get; } = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");

    /// <inheritdoc />
    public string SiteInstanceId { get; } = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");

    /// <inheritdoc />
    public bool IsRunningInAzureWebApp => !string.IsNullOrEmpty(HomeFolder) &&
                                          !string.IsNullOrEmpty(SiteName);
}
