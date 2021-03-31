// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Kestrel <see cref="IWebHostBuilder"/> extensions.
    /// </summary>
    public static class WebHostBuilderKestrelExtensions
    {
        /// <summary>
        /// Specify Kestrel as the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                // Don't override an already-configured transport
                services.TryAddSingleton<IConnectionListenerFactory, SocketTransportFactory>();

                services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>();
                services.AddSingleton<IServer, KestrelServerImpl>();
            });
        }

        /// <summary>
        /// Specify Kestrel as the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <param name="options">
        /// A callback to configure Kestrel options.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder, Action<KestrelServerOptions> options)
        {
            return hostBuilder.UseKestrel().ConfigureKestrel(options);
        }

        /// <summary>
        /// Configures Kestrel options but does not register an IServer. See <see cref="UseKestrel(IWebHostBuilder)"/>.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <param name="options">
        /// A callback to configure Kestrel options.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        public static IWebHostBuilder ConfigureKestrel(this IWebHostBuilder hostBuilder, Action<KestrelServerOptions> options)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.Configure(options);
            });
        }

        /// <summary>
        /// Specify Kestrel as the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <param name="configureOptions">A callback to configure Kestrel options.</param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, KestrelServerOptions> configureOptions)
        {
            return hostBuilder.UseKestrel().ConfigureKestrel(configureOptions);
        }

        /// <summary>
        /// Configures Kestrel options but does not register an IServer. See <see cref="UseKestrel(IWebHostBuilder)"/>.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <param name="configureOptions">A callback to configure Kestrel options.</param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        public static IWebHostBuilder ConfigureKestrel(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, KestrelServerOptions> configureOptions)
        {
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return hostBuilder.ConfigureServices((context, services) =>
            {
                services.Configure<KestrelServerOptions>(options =>
                {
                    configureOptions(context, options);
                });
            });
        }
    }
}
