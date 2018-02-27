// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Net.Mime;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public static class BlazorAppBuilderExtensions
    {
        /// <summary>
        /// Configures the middleware pipeline to work with Blazor.
        /// </summary>
        /// <typeparam name="TProgram">Any type from the client app project. This is used to identify the client app assembly.</typeparam>
        /// <param name="applicationBuilder"></param>
        public static void UseBlazor<TProgram>(
            this IApplicationBuilder applicationBuilder)
        {
            var clientAssemblyInServerBinDir = typeof(TProgram).Assembly;
            applicationBuilder.UseBlazor(new BlazorOptions
            {
                ClientAssemblyPath = clientAssemblyInServerBinDir.Location,
            });
        }

        /// <summary>
        /// Configures the middleware pipeline to work with Blazor.
        /// </summary>
        /// <param name="applicationBuilder"></param>
        /// <param name="options"></param>
        public static void UseBlazor(
            this IApplicationBuilder applicationBuilder,
            BlazorOptions options)
        {
            var config = BlazorConfig.Read(options.ClientAssemblyPath);
            var clientAppBinDir = Path.GetDirectoryName(config.SourceOutputAssemblyPath);
            var clientAppDistDir = Path.Combine(clientAppBinDir, "dist");
            var distDirStaticFiles = new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(clientAppDistDir),
                ContentTypeProvider = CreateContentTypeProvider(),
            };

            // First, match the request against files in the client app dist directory
            applicationBuilder.UseStaticFiles(distDirStaticFiles);

            // Next, match the request against static files in wwwroot
            if (!string.IsNullOrEmpty(config.WebRootPath))
            {
                // In development, we serve the wwwroot files directly from source
                // (and don't require them to be copied into dist).
                // TODO: When publishing is implemented, have config.WebRootPath set
                // to null so that it only serves files that were copied to dist
                applicationBuilder.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(config.WebRootPath)
                });
            }

            // Finally, use SPA fallback routing (serve default page for anything else,
            // excluding /_framework/*)
            applicationBuilder.MapWhen(IsNotFrameworkDir, childAppBuilder =>
            {
                childAppBuilder.UseSpa(spa =>
                {
                    spa.Options.DefaultPageStaticFileOptions = distDirStaticFiles;
                });
            });
        }

        private static bool IsNotFrameworkDir(HttpContext context)
            => !context.Request.Path.StartsWithSegments("/_framework");

        private static IContentTypeProvider CreateContentTypeProvider()
        {
            var result = new FileExtensionContentTypeProvider();
            result.Mappings.Add(".dll", MediaTypeNames.Application.Octet);
            result.Mappings.Add(".mem", MediaTypeNames.Application.Octet);
            result.Mappings.Add(".wasm", MediaTypeNames.Application.Octet);
            return result;
        }
    }
}
