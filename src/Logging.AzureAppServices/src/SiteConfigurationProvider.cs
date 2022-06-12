// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.AzureAppServices;

internal sealed class SiteConfigurationProvider
{
    public static IConfiguration GetAzureLoggingConfiguration(IWebAppContext context)
    {
        var settingsFolder = Path.Combine(context.HomeFolder, "site", "diagnostics");
        var settingsFile = Path.Combine(settingsFolder, "settings.json");

        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile(settingsFile, optional: true, reloadOnChange: true)
            .Build();
    }
}
