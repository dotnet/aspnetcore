// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to configure an <see cref="IApplicationBuilder"/> for serving interactive components.
    /// </summary>
    public static class RazorComponentsApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds middleware for serving interactive Razor Components.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <typeparam name="TStartup">A components app startup type.</typeparam>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseRazorComponents<TStartup>(
            this IApplicationBuilder builder)
        {
            return UseRazorComponents<TStartup>(builder, null);
        }

        /// <summary>
        /// Adds middleware for serving interactive Razor Components.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="configure">A callback that can be used to configure the middleware.</param>
        /// <typeparam name="TStartup">A components app startup type.</typeparam>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseRazorComponents<TStartup>(
            this IApplicationBuilder builder,
            Action<RazorComponentsOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var options = new RazorComponentsOptions();
            configure?.Invoke(options);

            // The use case for this flag is when developers want to add their own
            // SignalR middleware, e.g., when using Azure SignalR. By default we
            // add SignalR and BlazorHub automatically.
            if (options.UseSignalRWithBlazorHub)
            {
                builder.UseSignalR(route => route.MapHub<ComponentsHub>(ComponentsHub.DefaultPath));
            }

            // Use embedded static content for /_framework
            builder.Map("/_framework", frameworkBuilder =>
            {
                UseFrameworkFiles(frameworkBuilder);
            });

            // Use SPA fallback routing for anything else
            builder.UseSpa(spa => { });

            return builder;
        }

        private static void UseFrameworkFiles(IApplicationBuilder builder)
        {
            builder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(
                    typeof(RazorComponentsApplicationBuilderExtensions).Assembly,
                    "frameworkFiles"),
                OnPrepareResponse = BlazorApplicationBuilderExtensions.SetCacheHeaders
            });

            // TODO: Remove this
            // This is needed temporarily only until we implement a proper version
            // of library-embedded static resources for Razor Components apps.
            builder.Map("/blazor.boot.json", bootJsonBuilder =>
            {
                bootJsonBuilder.Use(async (ctx, next) =>
                {
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync(@"{ ""cssReferences"": [], ""jsReferences"": [] }");
                });
            });
        }
    }
}
