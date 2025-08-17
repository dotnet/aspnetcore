// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring the <see cref="IHostBuilder" />.
/// </summary>
public static class GenericHostBuilderExtensions
{
    /// <summary>
    /// Configures a <see cref="IHostBuilder" /> with defaults for hosting a web app. This should be called
    /// before application specific configuration to avoid it overwriting provided services, configuration sources,
    /// environments, content root, etc.
    /// </summary>
    /// <remarks>
    /// The following defaults are applied to the <see cref="IHostBuilder"/>:
    /// <list type="bullet">
    ///     <item><description>use Kestrel as the web server and configure it using the application's configuration providers</description></item>
    ///     <item><description>configure <see cref="IWebHostEnvironment.WebRootFileProvider"/> to include static web assets from projects referenced by the entry assembly during development</description></item>
    ///     <item><description>adds the HostFiltering middleware</description></item>
    ///     <item><description>adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,</description></item>
    ///     <item><description>enable IIS integration</description></item>
    ///   </list>
    /// </remarks>
    /// <param name="builder">The <see cref="IHostBuilder" /> instance to configure.</param>
    /// <param name="configure">The configure callback</param>
    /// <returns>A reference to the <paramref name="builder"/> after the operation has completed.</returns>
    public static IHostBuilder ConfigureWebHostDefaults(this IHostBuilder builder, Action<IWebHostBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ConfigureWebHostDefaults(configure, _ => { });
    }

    /// <summary>
    /// Configures a <see cref="IHostBuilder" /> with defaults for hosting a web app. This should be called
    /// before application specific configuration to avoid it overwriting provided services, configuration sources,
    /// environments, content root, etc.
    /// </summary>
    /// <remarks>
    /// The following defaults are applied to the <see cref="IHostBuilder"/>:
    /// <list type="bullet">
    ///     <item><description>use Kestrel as the web server and configure it using the application's configuration providers</description></item>
    ///     <item><description>configure <see cref="IWebHostEnvironment.WebRootFileProvider"/> to include static web assets from projects referenced by the entry assembly during development</description></item>
    ///     <item><description>adds the HostFiltering middleware</description></item>
    ///     <item><description>adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,</description></item>
    ///     <item><description>enable IIS integration</description></item>
    ///   </list>
    /// </remarks>
    /// <param name="builder">The <see cref="IHostBuilder" /> instance to configure.</param>
    /// <param name="configure">The configure callback</param>
    /// <param name="configureOptions">The delegate that configures the <see cref="WebHostBuilderOptions"/>.</param>
    /// <returns>A reference to the <paramref name="builder"/> after the operation has completed.</returns>
    public static IHostBuilder ConfigureWebHostDefaults(this IHostBuilder builder, Action<IWebHostBuilder> configure, Action<WebHostBuilderOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ConfigureWebHost(webHostBuilder =>
        {
            WebHost.ConfigureWebDefaults(webHostBuilder);

            configure(webHostBuilder);
        }, configureOptions);
    }
}
