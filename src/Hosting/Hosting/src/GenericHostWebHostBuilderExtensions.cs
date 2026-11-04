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
        ArgumentNullException.ThrowIfNull(configure);

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
        return ConfigureWebHost(
            builder,
            static (hostBuilder, options) => new GenericWebHostBuilder(hostBuilder, options),
            configure,
            configureWebHostBuilder);
    }

    /// <summary>
    /// Adds and configures an ASP.NET Core web application with minimal dependencies.
    /// </summary>
    /// <param name="builder">The <see cref="IHostBuilder"/> to add the <see cref="IWebHostBuilder"/> to.</param>
    /// <param name="configure">The delegate that configures the <see cref="IWebHostBuilder"/>.</param>
    /// <param name="configureWebHostBuilder">The delegate that configures the <see cref="WebHostBuilderOptions"/>.</param>
    /// <returns>The <see cref="IHostBuilder"/>.</returns>
    public static IHostBuilder ConfigureSlimWebHost(this IHostBuilder builder, Action<IWebHostBuilder> configure, Action<WebHostBuilderOptions> configureWebHostBuilder)
    {
        return ConfigureWebHost(
            builder,
            static (hostBuilder, options) => new SlimWebHostBuilder(hostBuilder, options),
            configure,
            configureWebHostBuilder);
    }

    private static IHostBuilder ConfigureWebHost(
        this IHostBuilder builder,
        Func<IHostBuilder, WebHostBuilderOptions, IWebHostBuilder> createWebHostBuilder,
        Action<IWebHostBuilder> configure,
        Action<WebHostBuilderOptions> configureWebHostBuilder)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(configureWebHostBuilder);

        // Light up custom implementations namely ConfigureHostBuilder which throws.
        if (builder is ISupportsConfigureWebHost supportsConfigureWebHost)
        {
            return supportsConfigureWebHost.ConfigureWebHost(configure, configureWebHostBuilder);
        }

        var webHostBuilderOptions = new WebHostBuilderOptions();
        configureWebHostBuilder(webHostBuilderOptions);
        var webhostBuilder = createWebHostBuilder(builder, webHostBuilderOptions);
        configure(webhostBuilder);
        builder.ConfigureServices((context, services) => services.AddHostedService<GenericWebHostService>());
        return builder;
    }
}
