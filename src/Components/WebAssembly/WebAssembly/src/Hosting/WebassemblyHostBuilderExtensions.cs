// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

/// <summary>
/// Extension methods for configuring a <see cref="WebAssemblyHostBuilder"/>.
/// </summary>
public static class WebAssemblyHostBuilderExtensions
{
    /// <summary>
    /// Configures the default service provider for the WebAssembly host.
    /// </summary>
    /// <param name="builder">The <see cref="WebAssemblyHostBuilder"/> to configure.</param>
    /// <param name="configure">A callback used to configure the <see cref="ServiceProviderOptions"/>.</param>
    /// <returns>The <see cref="WebAssemblyHostBuilder"/>.</returns>
    public static WebAssemblyHostBuilder UseDefaultServiceProvider(
        this WebAssemblyHostBuilder builder,
        Action<ServiceProviderOptions> configure)
    {
        return builder.UseDefaultServiceProvider((env, options) => configure(options));
    }

    /// <summary>
    /// Configures the default service provider for the WebAssembly host.
    /// </summary>
    /// <param name="builder">The <see cref="WebAssemblyHostBuilder"/> to configure.</param>
    /// <param name="configure">A callback used to configure the <see cref="ServiceProviderOptions"/> with access to the host environment.</param>
    /// <returns>The <see cref="WebAssemblyHostBuilder"/>.</returns>
    public static WebAssemblyHostBuilder UseDefaultServiceProvider(
        this WebAssemblyHostBuilder builder,
        Action<IWebAssemblyHostEnvironment, ServiceProviderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ServiceProviderOptions();
        configure(builder.HostEnvironment, options);

        return builder.UseServiceProviderOptions(options);
    }
}
