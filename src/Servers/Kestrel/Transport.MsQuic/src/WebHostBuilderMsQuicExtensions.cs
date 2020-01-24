// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic;
using Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderMsQuicExtensions
    {
        public static IWebHostBuilder UseMsQuic(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IConnectionListenerFactory, MsQuicTransportFactory>();
            });
        }

        public static IWebHostBuilder UseMsQuic(this IWebHostBuilder hostBuilder, Action<MsQuicTransportOptions> configureOptions)
        {
            return hostBuilder.UseMsQuic().ConfigureServices(services =>
            {
                services.Configure(configureOptions);
            });
        }
    }
}
