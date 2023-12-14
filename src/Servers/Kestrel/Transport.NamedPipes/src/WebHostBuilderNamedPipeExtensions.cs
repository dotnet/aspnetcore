// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// <see cref="IWebHostBuilder" /> extension methods to configure the Named Pipes transport to be used by Kestrel.
/// </summary>
public static class WebHostBuilderNamedPipeExtensions
{
    /// <summary>
    /// Specify Named Pipes as the transport to be used by Kestrel.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    [SupportedOSPlatform("windows")]
    public static IWebHostBuilder UseNamedPipes(this IWebHostBuilder hostBuilder)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Named pipes transport requires a Windows operating system.");
        }

        hostBuilder.ConfigureServices(services =>
        {
            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton<IConnectionListenerFactory, NamedPipeTransportFactory>();
        });
        return hostBuilder;
    }

    /// <summary>
    /// Specify Named Pipes as the transport to be used by Kestrel.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="configureOptions">A callback to configure transport options.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    [SupportedOSPlatform("windows")]
    public static IWebHostBuilder UseNamedPipes(this IWebHostBuilder hostBuilder, Action<NamedPipeTransportOptions> configureOptions)
    {
        return hostBuilder.UseNamedPipes().ConfigureServices(services =>
        {
            services.Configure(configureOptions);
        });
    }
}
