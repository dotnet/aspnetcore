// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extension method to add Azure AppServices integration to the app.
/// </summary>
public static class AppServicesWebHostBuilderExtensions
{
    /// <summary>
    /// Configures application to use Azure AppServices integration.
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    public static IWebHostBuilder UseAzureAppServices(this IWebHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
#pragma warning disable 618
        hostBuilder.ConfigureLogging(builder => builder.AddAzureWebAppDiagnostics());
#pragma warning restore 618
        return hostBuilder;
    }
}
