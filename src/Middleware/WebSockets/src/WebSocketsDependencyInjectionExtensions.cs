// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.WebSockets
{
    /// <summary>
    /// Extension method for <see cref="IServiceCollection"/> to add WebSockets configuration.
    /// </summary>
    public static class WebSocketsDependencyInjectionExtensions
    {
        /// <summary>
        /// Extension method for <see cref="IServiceCollection"/> to add WebSockets configuration.
        /// </summary>
        /// <param name="services">The service collection to add WebSockets specific configuration to.</param>
        /// <param name="configure">The configuration callback to setup <see cref="WebSocketOptions"/>.</param>
        /// <returns></returns>
        public static IServiceCollection AddWebSockets(this IServiceCollection services, Action<WebSocketOptions> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return services.Configure(configure);
        }
    }
}
