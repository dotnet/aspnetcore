// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConnectionsDependencyInjectionExtensions
    {
        public static IServiceCollection AddConnections(this IServiceCollection services)
        {
            services.AddRouting();
            services.AddAuthorizationPolicyEvaluator();
            services.TryAddSingleton<HttpConnectionDispatcher>();
            services.TryAddSingleton<HttpConnectionManager>();
            return services;
        }
    }
}
