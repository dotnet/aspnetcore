// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.OpenSSL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// <see cref="IWebHostBuilder" /> extension methods to configure the Socket transport to be used by Kestrel.
/// </summary>
public static class WebHostBuilderSocketExtensions
{
    /// <summary>
    /// Specify Sockets as the transport to be used by Kestrel.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder UseSockets(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(services =>
        {
            services.TryAddSingleton<IConnectionListenerFactory, SocketTransportFactory>();

            services.TryAddSingleton<IMemoryPoolFactory<byte>, DefaultSimpleMemoryPoolFactory>();
            services.AddOptions<SocketTransportOptions>().Configure((SocketTransportOptions options, IMemoryPoolFactory<byte> factory) =>
            {
                // Set the IMemoryPoolFactory from DI on SocketTransportOptions. Usually this should be the PinnedBlockMemoryPoolFactory from UseKestrelCore.
                options.MemoryPoolFactory = factory;
            });
        });
    }

    /// <summary>
    /// Specify Sockets as the transport to be used by Kestrel.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <param name="configureOptions">
    /// A callback to configure transport options.
    /// </param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder UseSockets(this IWebHostBuilder hostBuilder, Action<SocketTransportOptions> configureOptions)
    {
        return hostBuilder.UseSockets().ConfigureServices(services =>
        {
            services.Configure(configureOptions);
        });
    }

    /// <summary>
    /// Specify Direct Socket Transport with native OpenSSL integration as the transport to be used by Kestrel.
    /// This bypasses SslStream and integrates OpenSSL directly at the socket transport layer for zero-copy TLS processing.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    /// <remarks>
    /// This transport is experimental and bypasses the traditional HttpsConnectionMiddleware.
    /// It requires OpenSSL (libssl) to be available on the system.
    /// For HTTPS endpoints, you must call ConfigureHttpsDefaults or set certificates on endpoints manually.
    /// </remarks>
    public static IWebHostBuilder UseDirectSocketTransport(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(services =>
        {
            // Replace the default SocketTransportFactory with DirectSocketTransportFactory
            // We use AddSingleton here (not TryAddSingleton) to ensure it replaces any existing factory
            services.AddSingleton<IConnectionListenerFactory, DirectSocketTransportFactory>();

            services.TryAddSingleton<IMemoryPoolFactory<byte>, DefaultSimpleMemoryPoolFactory>();
            services.AddOptions<SocketTransportOptions>().Configure((SocketTransportOptions options, IMemoryPoolFactory<byte> factory) =>
            {
                // Set the IMemoryPoolFactory from DI on SocketTransportOptions. Usually this should be the PinnedBlockMemoryPoolFactory from UseKestrelCore.
                options.MemoryPoolFactory = factory;
            });
        });
    }

    /// <summary>
    /// Specify Direct Socket Transport with native OpenSSL integration and configure transport options.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <param name="configureOptions">
    /// A callback to configure transport options.
    /// </param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder UseDirectSocketTransport(this IWebHostBuilder hostBuilder, Action<SocketTransportOptions> configureOptions)
    {
        return hostBuilder.UseDirectSocketTransport().ConfigureServices(services =>
        {
            services.Configure(configureOptions);
        });
    }
}
