// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;

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
            // TODO: Make the .blazor.config file contents sane
            // Currently the items in it are bizarre and don't relate to their purpose,
            // hence all the path manipulation here. We shouldn't be hardcoding 'dist' here either.
            var env = (IHostingEnvironment)applicationBuilder.ApplicationServices.GetService(typeof(IHostingEnvironment));
            var config = BlazorConfig.Read(options.ClientAssemblyPath);
            var distDirStaticFiles = new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(config.DistPath),
                ContentTypeProvider = CreateContentTypeProvider(),
                OnPrepareResponse = SetCacheHeaders
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
                    FileProvider = new PhysicalFileProvider(config.WebRootPath),
                    OnPrepareResponse = SetCacheHeaders
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

        private static void SetCacheHeaders(StaticFileResponseContext ctx)
        {
            // By setting "Cache-Control: no-cache", we're allowing the browser to store
            // a cached copy of the response, but telling it that it must check with the
            // server for modifications (based on Etag) before using that cached copy.
            // Longer term, we should generate URLs based on content hashes (at least
            // for published apps) so that the browser doesn't need to make any requests
            // for unchanged files.
            var headers = ctx.Context.Response.GetTypedHeaders();
            if (headers.CacheControl == null)
            {
                headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true
                };
            }
        }

        private static bool IsNotFrameworkDir(HttpContext context)
            => !context.Request.Path.StartsWithSegments("/_framework");

        private static IContentTypeProvider CreateContentTypeProvider()
        {
            var result = new FileExtensionContentTypeProvider();
            result.Mappings.Add(".dll", MediaTypeNames.Application.Octet);
            result.Mappings.Add(".mem", MediaTypeNames.Application.Octet);
            result.Mappings.Add(".wasm", WasmMediaTypeNames.Application.Wasm);
            return result;
        }
    }
}
