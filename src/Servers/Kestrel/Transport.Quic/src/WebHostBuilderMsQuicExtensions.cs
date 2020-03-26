// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderMsQuicExtensions
    {
        public static IWebHostBuilder UseQuic(this IWebHostBuilder hostBuilder)
        {
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
