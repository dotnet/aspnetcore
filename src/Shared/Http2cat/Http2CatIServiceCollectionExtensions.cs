// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace http2cat
{
    public static class Http2CatIServiceCollectionExtensions
    {
        public static IServiceCollection UseHttp2Cat(this IServiceCollection services, Action<Http2CatOptions> configureOptions)
        {
            services.AddSingleton<IConnectionFactory, SocketConnectionFactory>();
            services.AddSingleton<Http2CatHostedService>();
            services.Configure(configureOptions);
            return services;
        }

        public static Task RunHttp2CatAsync(this IHost host)
        {
            return host.Services.GetRequiredService<Http2CatHostedService>().RunAsync();
        }
    }
}
