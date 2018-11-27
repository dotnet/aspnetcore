// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to configure an <see cref="IApplicationBuilder"/> for Server-Side Blazor.
    /// These are just shorthand for combining UseSignalR with UseBlazor.
    /// </summary>
    public static class ServerSideBlazorApplicationBuilderExtensions
    {
        /// <summary>
        /// Registers Server-Side Blazor in the pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <typeparam name="TStartup">A Blazor startup type.</typeparam>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseServerSideBlazor<TStartup>(
            this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // WARNING: Don't add extra setup logic here. It's important for
            // UseServerSideBlazor just to be shorthand for UseSignalR+UseBlazor,
            // so that people who want to call those two manually instead can
            // also do so. That's needed for people using Azure SignalR.

            // TODO: Also allow configuring the endpoint path.
            return UseSignalRWithBlazorHub(builder, BlazorHub.DefaultPath)
                .UseBlazor<TStartup>();
        }

        /// <summary>
        /// Registers Server-Side Blazor in the pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="options">A <see cref="BlazorOptions"/> instance used to configure the Blazor file provider.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseServerSideBlazor(
            this IApplicationBuilder builder,
            BlazorOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // WARNING: Don't add extra setup logic here. It's important for
            // UseServerSideBlazor just to be shorthand for UseSignalR+UseBlazor,
            // so that people who want to call those two manually instead can
            // also do so. That's needed for people using Azure SignalR.

            // TODO: Also allow configuring the endpoint path.
            return UseSignalRWithBlazorHub(builder, BlazorHub.DefaultPath)
                .UseBlazor(options);
        }

        private static IApplicationBuilder UseSignalRWithBlazorHub(
            IApplicationBuilder builder, PathString path)
        {
            return builder.UseSignalR(route => route.MapHub<BlazorHub>(BlazorHub.DefaultPath));
        }
    }
}
