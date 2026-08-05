// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// <see cref="IWebHostBuilder"/> extension methods to register the DirectTls transport with Kestrel.
/// </summary>
[Experimental("ASPNETCORE_DIRECTTLS_001", UrlFormat = "https://aka.ms/aspnetcore/directtls")]
public static class WebHostBuilderDirectTlsExtensions
{
    /// <summary>
    /// Registers the DirectTls transport (native, file-descriptor-bound OpenSSL TLS) alongside the default
    /// Kestrel transport. Endpoints bound with a <see cref="DirectTlsEndpoint"/> are served by this transport;
    /// all other endpoints continue to use the default transport.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    /// <remarks>
    /// Call this <b>after</b> <c>UseKestrel()</c> so the DirectTls transport is offered a
    /// <see cref="DirectTlsEndpoint"/> before the default transport.
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public static IWebHostBuilder UseDirectTls(this IWebHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("The DirectTls transport requires a Linux operating system.");
        }

        return hostBuilder.ConfigureServices(services =>
        {
            // Registered additively (not TryAdd): the default transport is already registered by UseKestrel.
            // KestrelServerImpl reverses the factory list, so this last-registered factory is tried first and
            // its CanBind claims only DirectTlsEndpoint; every other endpoint falls through to the default.
            services.AddSingleton<IConnectionListenerFactory, DirectTlsTransportFactory>();

            services.TryAddSingleton<IMemoryPoolFactory<byte>, DefaultSimpleMemoryPoolFactory>();
            services.AddOptions<DirectTlsTransportOptions>().Configure((DirectTlsTransportOptions options, IMemoryPoolFactory<byte> factory) =>
            {
                // UseKestrelCore normally registers PinnedBlockMemoryPoolFactory. The fallback supports
                // standalone transport registration, matching the sockets transport.
                options.MemoryPoolFactory = factory;
            });

            // Copies each DirectTlsEndpoint's ListenOptions.Protocols onto the endpoint after all endpoints
            // are configured, so the transport can read the ALPN protocols off the endpoint itself.
            services.AddSingleton<IPostConfigureOptions<KestrelServerOptions>, DirectTlsEndpointProtocolsSetup>();
        });
    }

    /// <summary>
    /// Registers the DirectTls transport and configures its transport-wide options.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="configureOptions">A callback to configure <see cref="DirectTlsTransportOptions"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    [SupportedOSPlatform("linux")]
    public static IWebHostBuilder UseDirectTls(this IWebHostBuilder hostBuilder, Action<DirectTlsTransportOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return hostBuilder.UseDirectTls().ConfigureServices(services =>
        {
            services.Configure(configureOptions);
        });
    }
}
