// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;

namespace Microsoft.AspNetCore.Builder
{
    public static class BlazorAppBuilderExtensions
    {
        public static void UseBlazor(
            this IApplicationBuilder applicationBuilder,
            string clientAssemblyName)
        {
            var binDir = Path.GetDirectoryName(typeof(BlazorConfig).Assembly.Location);
            var clientAssemblyPath = Path.Combine(binDir, $"{clientAssemblyName}.dll");
            applicationBuilder.UseBlazorInternal(clientAssemblyPath);
        }
        
        // TODO: Change this combination of APIs to make it possible to supply either
        // an assembly name (resolved to current bin dir) or full assembly path
        internal static void UseBlazorInternal(
            this IApplicationBuilder applicationBuilder,
            string clientAssemblyPath)
        {
            var config = BlazorConfig.Read(clientAssemblyPath);
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
