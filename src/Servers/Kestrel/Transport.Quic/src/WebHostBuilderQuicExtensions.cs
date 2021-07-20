// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Quic;
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
        public static IWebHostBuilder UseQuic(this IWebHostBuilder hostBuilder)
        {
            if (!QuicImplementationProviders.Default.IsSupported)
            {
                throw new NotSupportedException("QUIC is not supported or enabled on this platform. See https://aka.ms/aspnet/kestrel/http3reqs for details.");
            }
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IMultiplexedConnectionListenerFactory, QuicTransportFactory>();
            });
        }

        public static IWebHostBuilder UseQuic(this IWebHostBuilder hostBuilder, Action<QuicTransportOptions> configureOptions)
        {
            return hostBuilder.UseQuic().ConfigureServices(services =>
            {
                services.Configure(configureOptions);
            });
        }
    }
}
