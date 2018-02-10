// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System;

namespace Microsoft.AspNetCore.Builder
{
    public static class BlazorAppBuilderExtensions
    {
        /// <summary>
        /// Configures the middleware pipeline to work with Blazor.
        /// </summary>
        /// <param name="applicationBuilder"></param>
        /// <param name="clientAssemblyName"
        ///     >The name of the client assembly relative to the current bin directory.</param>
        public static void UseBlazor(
            this IApplicationBuilder applicationBuilder,
            string clientAssemblyName)
        {
            var binDir = Path.GetDirectoryName(typeof(BlazorConfig).Assembly.Location);
            var clientAssemblyPath = Path.Combine(binDir, $"{clientAssemblyName}.dll");
            applicationBuilder.UseBlazor(new BlazorOptions
            {
                ClientAssemblyPath = clientAssemblyPath,
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
            var distFileProvider = new PhysicalFileProvider(clientAppDistDir);

            applicationBuilder.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = distFileProvider
            });

            applicationBuilder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = distFileProvider,
                ContentTypeProvider = CreateContentTypeProvider(),
            });

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
        }

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
