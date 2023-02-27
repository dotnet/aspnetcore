// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// <see cref="IWebHostBuilder" /> extension methods to configure the Quic transport to be used by Kestrel.
/// </summary>
public static class WebHostBuilderQuicExtensions
{
    /// <summary>
    /// Specify Quic as the transport to be used by Kestrel.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseQuic(this IWebHostBuilder hostBuilder)
    {
        // In order to be able to provide useful error messages in slim scenarios, we have to be able
        // to distinguish between QUIC-was-not-requested and QUIC-is-not-available.
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<MultiplexedConnectionMarkerService>();
        });

        if (QuicListener.IsSupported)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IMultiplexedConnectionListenerFactory, QuicTransportFactory>();
            });
        }

        return hostBuilder;
    }

    /// <summary>
    /// Specify Quic as the transport to be used by Kestrel.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="configureOptions">A callback to configure transport options.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseQuic(this IWebHostBuilder hostBuilder, Action<QuicTransportOptions> configureOptions)
    {
        return hostBuilder.UseQuic().ConfigureServices(services =>
        {
            services.Configure(configureOptions);
        });
    }
}
