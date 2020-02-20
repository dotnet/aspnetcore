// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Mime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for mapping Blazor WebAssembly applications.
    /// </summary>
    public static class ComponentsWebAssemblyApplicationBuilderExtensions
    {
        /// <summary>
        /// Maps a Blazor webassembly application to the <paramref name="pathPrefix"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="pathPrefix">The <see cref="PathString"/> that indicates the prefix for the Blazor application.</param>
        /// <returns>The <see cref="IApplicationBuilder"/></returns>
        public static IApplicationBuilder MapBlazorFrameworkFiles(this IApplicationBuilder builder, PathString pathPrefix)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var webHostEnvironment = builder.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            var options = CreateStaticFilesOptions(webHostEnvironment.WebRootFileProvider);

            builder.MapWhen(ctx => ctx.Request.Path.StartsWithSegments(pathPrefix, out var rest) && rest.StartsWithSegments("/_framework") && !rest.StartsWithSegments("/_framework/blazor.server.js"),
            subBuilder =>
            {
                subBuilder.Use(async (ctx, next) =>
                {
                    // At this point we mapped something from the /_framework
                    ctx.Response.Headers.Append(HeaderNames.CacheControl, "no-cache");
                    // This will invoke the static files middleware plugged-in below.
                    await next();
                });

                subBuilder.UseStaticFiles(options);
            });

            return builder;
        }

        /// <summary>
        /// Maps a Blazor webassembly application to the root path of the application "/".
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="pathPrefix">The <see cref="PathString"/> that indicates the prefix for the Blazor application.</param>
        /// <returns>The <see cref="IApplicationBuilder"/></returns>
        public static IApplicationBuilder UseBlazorFrameworkFiles(this IApplicationBuilder applicationBuilder) =>
            MapBlazorFrameworkFiles(applicationBuilder, default);

        private static StaticFileOptions CreateStaticFilesOptions(IFileProvider webRootFileProvider)
        {
            var options = new StaticFileOptions();
            options.FileProvider = webRootFileProvider;
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            AddMapping(contentTypeProvider, ".dll", MediaTypeNames.Application.Octet);
            // We unconditionally map pdbs as there will be no pdbs in the output folder for
            // release builds unless BlazorEnableDebugging is explicitly set to true.
            AddMapping(contentTypeProvider, ".pdb", MediaTypeNames.Application.Octet);

            options.ContentTypeProvider = contentTypeProvider;

            return options;
        }

        private static void AddMapping(FileExtensionContentTypeProvider provider, string name, string mimeType)
        {
            if (!provider.Mappings.ContainsKey(name))
            {
                provider.Mappings.Add(name, mimeType);
            }
        }
    }
}
