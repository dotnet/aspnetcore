// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

namespace Microsoft.AspNetCore.Hosting;

public static partial class WebHostBuilderSocketExtensions
{
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
    [Experimental("ASPNETCORE_DIRECTSSL_001")]
    public static IWebHostBuilder UseDirectSslSocketTransport(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(services =>
        {
            // Replace the default SocketTransportFactory with DirectSocketTransportFactory
            // We use AddSingleton here (not TryAddSingleton) to ensure it replaces any existing factory
            services.AddSingleton<IConnectionListenerFactory, DirectSslTransportFactory>();

            services.TryAddSingleton<IMemoryPoolFactory<byte>, DefaultSimpleMemoryPoolFactory>();
            services.AddOptions<SocketTransportOptions>().Configure((SocketTransportOptions options, IMemoryPoolFactory<byte> factory) =>
            {
                // Set the IMemoryPoolFactory from DI on SocketTransportOptions. Usually this should be the PinnedBlockMemoryPoolFactory from UseKestrelCore.
                options.MemoryPoolFactory = factory;
            });
        });
    }

    /// <summary>
    /// Specify DirectSsl Sockets with native OpenSSL integration as the transport to be used by Kestrel.
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
    [Experimental("ASPNETCORE_DIRECTSSL_001", UrlFormat = "https://aka.ms/aspnetcore/directssl")]
    public static IWebHostBuilder UseDirectSslSockets(this IWebHostBuilder hostBuilder, Action<DirectSslTransportOptions> configureOptions)
    {
        return hostBuilder.UseDirectSslSocketTransport().ConfigureServices(services =>
        {
            services.Configure(configureOptions);
        });
    }
}
