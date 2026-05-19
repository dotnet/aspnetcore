// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.Server.IISIntegration.IISHostingStartup))]

namespace Microsoft.AspNetCore.Server.IISIntegration;

/// <summary>
/// The <see cref="IHostingStartup"/> to add IISIntegration to apps.
/// </summary>
/// <remarks>
/// This API isn't meant to be used by user code.
/// </remarks>
public class IISHostingStartup : IHostingStartup
{
    /// <summary>
    /// Adds IISIntegration into the middleware pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    public void Configure(IWebHostBuilder builder)
    {
        builder.UseIISIntegration();
    }
}
