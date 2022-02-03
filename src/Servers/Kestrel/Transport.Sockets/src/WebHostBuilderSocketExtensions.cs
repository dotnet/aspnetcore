// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<IConnectionListenerFactory, SocketTransportFactory>();
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
}
