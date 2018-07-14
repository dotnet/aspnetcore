// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Builder;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.AspNetCore.Blazor.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to configure an <see cref="IApplicationBuilder"/> for Server-Side Blazor.
    /// </summary>
    public static class ServerSideBlazorApplicationBuilderExtensions
    {
        /// <summary>
        /// Registers Server-Side Blazor in the pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <typeparam name="TStartup">A Blazor startup type.</typeparam>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseServerSideBlazor<TStartup>(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return UseServerSideBlazor(builder, typeof(TStartup));
        }

        /// <summary>
        /// Registers Server-Side Blazor in the pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="startupType">A Blazor startup type.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseServerSideBlazor(
            this IApplicationBuilder builder,
            Type startupType)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (startupType == null)
            {
                throw new ArgumentNullException(nameof(startupType));
            }

            var startup = builder.ApplicationServices.GetRequiredService(startupType);
            var wrapper = new ConventionBasedStartup(startup);
            Action<IBlazorApplicationBuilder> configure = (b) =>
            {
                wrapper.Configure(b, b.Services);
            };

            UseServerSideBlazorCore(builder, configure);

            builder.UseBlazor(new BlazorOptions()
            {
                ClientAssemblyPath = startupType.Assembly.Location,
            });

            return builder;
        }

        /// <summary>
        /// Registers middleware for Server-Side Blazor.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="options">A <see cref="BlazorOptions"/> instance used to configure the Blazor file provider.</param>
        /// <param name="startupAction">A delegate used to configure the renderer.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseServerSideBlazor(
            this IApplicationBuilder builder,
            BlazorOptions options,
            Action<IBlazorApplicationBuilder> startupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (startupAction == null)
            {
                throw new ArgumentNullException(nameof(startupAction));
            }

            UseServerSideBlazorCore(builder, startupAction);

            builder.UseBlazor(options);

            return builder;
        }

        private static IApplicationBuilder UseServerSideBlazorCore(
            IApplicationBuilder builder,
            Action<IBlazorApplicationBuilder> configure)
        {
            var endpoint = "/_blazor";

            var factory = (DefaultCircuitFactory)builder.ApplicationServices.GetRequiredService<CircuitFactory>();
            factory.StartupActions.Add(endpoint, configure);

            builder.UseSignalR(route => route.MapHub<BlazorHub>(endpoint));

            return builder;
        }
    }
}
