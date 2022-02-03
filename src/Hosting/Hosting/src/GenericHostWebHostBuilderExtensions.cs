// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Contains extensions for an <see cref="IHostBuilder"/>.
/// </summary>
public static class GenericHostWebHostBuilderExtensions
{
    /// <summary>
    /// Adds and configures an ASP.NET Core web application.
    /// </summary>
    /// <param name="builder">The <see cref="IHostBuilder"/> to add the <see cref="IWebHostBuilder"/> to.</param>
    /// <param name="configure">The delegate that configures the <see cref="IWebHostBuilder"/>.</param>
    /// <returns>The <see cref="IHostBuilder"/>.</returns>
    public static IHostBuilder ConfigureWebHost(this IHostBuilder builder, Action<IWebHostBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureWebHost(configure, _ => { });
    }

    /// <summary>
    /// Adds and configures an ASP.NET Core web application.
    /// </summary>
    /// <param name="builder">The <see cref="IHostBuilder"/> to add the <see cref="IWebHostBuilder"/> to.</param>
    /// <param name="configure">The delegate that configures the <see cref="IWebHostBuilder"/>.</param>
    /// <param name="configureWebHostBuilder">The delegate that configures the <see cref="WebHostBuilderOptions"/>.</param>
    /// <returns>The <see cref="IHostBuilder"/>.</returns>
    public static IHostBuilder ConfigureWebHost(this IHostBuilder builder, Action<IWebHostBuilder> configure, Action<WebHostBuilderOptions> configureWebHostBuilder)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        if (configureWebHostBuilder is null)
        {
            throw new ArgumentNullException(nameof(configureWebHostBuilder));
        }

        // Light up custom implementations namely ConfigureHostBuilder which throws.
        if (builder is ISupportsConfigureWebHost supportsConfigureWebHost)
        {
            return supportsConfigureWebHost.ConfigureWebHost(configure, configureWebHostBuilder);
        }

        var webHostBuilderOptions = new WebHostBuilderOptions();
        configureWebHostBuilder(webHostBuilderOptions);
        var webhostBuilder = new GenericWebHostBuilder(builder, webHostBuilderOptions);
        configure(webhostBuilder);
        builder.ConfigureServices((context, services) => services.AddHostedService<GenericWebHostService>());
        return builder;
    }
}
