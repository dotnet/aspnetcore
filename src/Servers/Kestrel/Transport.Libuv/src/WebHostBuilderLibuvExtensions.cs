// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Libuv <see cref="IWebHostBuilder"/> extensions.
    /// </summary>
    [Obsolete("The libuv transport is obsolete and will be removed in a future release. See https://aka.ms/libuvtransport for details.", error: false)] // Remove after .NET 6.
    public static class WebHostBuilderLibuvExtensions
    {
        /// <summary>
        /// Specify Libuv as the transport to be used by Kestrel.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        [Obsolete("The libuv transport is obsolete and will be removed in a future release. See https://aka.ms/libuvtransport for details.", error: false)] // Remove after .NET 6.
        public static IWebHostBuilder UseLibuv(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IConnectionListenerFactory, LibuvTransportFactory>();
            });
        }

        /// <summary>
        /// Specify Libuv as the transport to be used by Kestrel.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <param name="configureOptions">
        /// A callback to configure Libuv options.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        [Obsolete("The libuv transport is obsolete and will be removed in a future release. See https://aka.ms/libuvtransport for details.", error: false)] // Remove after .NET 6.
        public static IWebHostBuilder UseLibuv(this IWebHostBuilder hostBuilder, Action<LibuvTransportOptions> configureOptions)
        {
            return hostBuilder.UseLibuv().ConfigureServices(services =>
            {
                services.Configure(configureOptions);
            });
        }
    }
}
