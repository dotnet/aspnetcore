// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Quic;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Quic <see cref="IWebHostBuilder"/> extensions.
    /// </summary>
    public static class WebHostBuilderQuicExtensions
    {
        [RequiresPreviewFeatures]
        public static IWebHostBuilder UseQuic(this IWebHostBuilder hostBuilder)
        {
            if (QuicImplementationProviders.Default.IsSupported)
            {
                return hostBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton<IMultiplexedConnectionListenerFactory, QuicTransportFactory>();
                });
            }

            return hostBuilder;
        }

        [RequiresPreviewFeatures]
        public static IWebHostBuilder UseQuic(this IWebHostBuilder hostBuilder, Action<QuicTransportOptions> configureOptions)
        {
            return hostBuilder.UseQuic().ConfigureServices(services =>
            {
                services.Configure(configureOptions);
            });
        }
    }
}
