// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Blazor.Server.FrameworkFiles;
using Microsoft.Blazor.Server.WebRootFiles;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;

namespace Microsoft.AspNetCore.Builder
{
    public static class BlazorAppBuilderExtensions
    {
        public static void UseBlazor(
            this IApplicationBuilder applicationBuilder,
            string assemblyPath,
            string staticFilesRoot)
        {
            var frameworkFileProvider = FrameworkFileProvider.Instantiate(assemblyPath);

            if (staticFilesRoot != null)
            {
                var env = applicationBuilder.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                var clientWebRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, staticFilesRoot));
                var webRootFileProvider = WebRootFileProvider.Instantiate(
                    clientWebRoot,
                    Path.GetFileNameWithoutExtension(assemblyPath),
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
