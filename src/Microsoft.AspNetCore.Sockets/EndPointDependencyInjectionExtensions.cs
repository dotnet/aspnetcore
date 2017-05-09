// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EndPointDependencyInjectionExtensions
    {
        public static IServiceCollection AddEndPoint<TEndPoint>(this IServiceCollection services) where TEndPoint : EndPoint
        {
            services.AddSingleton<TEndPoint>();

            return services;
        }

        public static IServiceCollection AddEndPoint<TEndPoint>(this IServiceCollection services,
            Action<EndPointOptions<TEndPoint>> setupAction) where TEndPoint : EndPoint
        {
            services.AddEndPoint<TEndPoint>();

            services.Configure(setupAction);

            return services;
        }
    }
}
