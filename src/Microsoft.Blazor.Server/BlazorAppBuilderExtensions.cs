// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Blazor.Server;
using Microsoft.Blazor.Server.FrameworkFiles;
using Microsoft.Blazor.Server.WebRootFiles;
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
            var frameworkFileProvider = FrameworkFileProvider.Instantiate(
                config.SourceOutputAssemblyPath);

            if (config.WebRootPath != null)
            {
                var webRootFileProvider = WebRootFileProvider.Instantiate(
                    config.WebRootPath,
                    Path.GetFileNameWithoutExtension(config.SourceOutputAssemblyPath),
                    frameworkFileProvider.GetDirectoryContents("/_bin"));

                applicationBuilder.UseDefaultFiles(new DefaultFilesOptions
                {
                    FileProvider = webRootFileProvider
                });

                applicationBuilder.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = webRootFileProvider
                });
            }
            
            applicationBuilder.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = "/_framework",
                FileProvider = frameworkFileProvider,
                ContentTypeProvider = CreateContentTypeProvider(),
            });
        }

        private static IContentTypeProvider CreateContentTypeProvider()
        {
            return new FileExtensionContentTypeProvider(new Dictionary<string, string>
            {
                { ".dll", MediaTypeNames.Application.Octet },
                { ".js", "application/javascript" },
                { ".mem", MediaTypeNames.Application.Octet },
                { ".wasm", MediaTypeNames.Application.Octet },
            });
        }
    }
}
