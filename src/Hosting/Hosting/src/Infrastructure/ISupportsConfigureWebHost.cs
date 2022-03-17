// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting.Infrastructure;

/// <summary>
/// An interface implemented by IWebHostBuilders that handle <see cref="GenericHostWebHostBuilderExtensions.ConfigureWebHost(IHostBuilder, Action{IWebHostBuilder})"/>
/// directly.
/// </summary>
public interface ISupportsConfigureWebHost
{
    /// <summary>
    /// Adds and configures an ASP.NET Core web application.
    /// </summary>
    /// <param name="configure">The delegate that configures the <see cref="IWebHostBuilder"/>.</param>
    /// <param name="configureOptions">The delegate that configures the <see cref="WebHostBuilderOptions"/>.</param>
    /// <returns>The <see cref="IHostBuilder"/>.</returns>
    IHostBuilder ConfigureWebHost(Action<IWebHostBuilder> configure, Action<WebHostBuilderOptions> configureOptions);
}
