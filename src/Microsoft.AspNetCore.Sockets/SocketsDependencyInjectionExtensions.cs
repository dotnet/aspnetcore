// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SocketsDependencyInjectionExtensions
    {
        public static IServiceCollection AddSockets(this IServiceCollection services)
        {
            services.AddRouting();
            services.TryAddSingleton<ConnectionManager>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, SocketsApplicationLifetimeService>());
            services.TryAddSingleton<HttpConnectionDispatcher>();
            return services;
        }
    }
}
