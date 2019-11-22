// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides options for configuring Server-Side Blazor.
    /// </summary>
    public static class ServerSideBlazorBuilderExtensions
    {
        /// <summary>
        /// Adds options to configure circuits.
        /// </summary>
        /// <param name="builder">The <see cref="IServerSideBlazorBuilder"/>.</param>
        /// <param name="configure">A callback to configure <see cref="CircuitOptions"/>.</param>
        /// <returns>The <see cref="IServerSideBlazorBuilder"/>.</returns>
        public static IServerSideBlazorBuilder AddCircuitOptions(this IServerSideBlazorBuilder builder, Action<CircuitOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure<CircuitOptions>(configure);

            return builder;
        }

        /// <summary>
        /// Adds hub options for the configuration of the SignalR Hub used by Server-Side Blazor.
        /// </summary>
        /// <param name="builder">The <see cref="IServerSideBlazorBuilder"/>.</param>
        /// <param name="configure">A callback to configure the hub options.</param>
        /// <returns>The <see cref="IServerSideBlazorBuilder"/>.</returns>
        public static IServerSideBlazorBuilder AddHubOptions(this IServerSideBlazorBuilder builder, Action<HubOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure != null)
            {
                builder.Services.Configure<HubOptions<ComponentHub>>(configure);
            }

            return builder;
        }
    }
}
