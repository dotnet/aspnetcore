// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ConnectionsDependencyInjectionExtensions
    {
        /// <summary>
        /// Adds required services for ASP.NET Core Connection Handlers to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
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
