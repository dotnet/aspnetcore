// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extension methods for the IIS In-Process.
/// </summary>
public static class WebHostBuilderIISExtensions
{
    /// <summary>
    /// Configures the port and base path the server should listen on when running behind AspNetCoreModule.
    /// The app will also be configured to capture startup errors.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseIIS(this IWebHostBuilder hostBuilder) => hostBuilder;
}
