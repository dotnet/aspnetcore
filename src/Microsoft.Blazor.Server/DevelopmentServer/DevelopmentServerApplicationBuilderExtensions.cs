// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Microsoft.AspNetCore.Builder
{
    public static class DevelopmentServerApplicationBuilderExtensions
    {
        public static void UseBlazorDevelopmentServer(
            this IApplicationBuilder applicationBuilder,
            string relativeSourcePath)
        {
            var env = applicationBuilder.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            var sourcePath = Path.Combine(env.ContentRootPath, relativeSourcePath);
            ServeWebRoot(applicationBuilder, sourcePath);
        }

        private static void ServeWebRoot(IApplicationBuilder applicationBuilder, string clientAppSourceRoot)
        {
            var webRootFileProvider = new PhysicalFileProvider(
                Path.Combine(clientAppSourceRoot, "wwwroot"));

            applicationBuilder.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = webRootFileProvider
            });

            applicationBuilder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = webRootFileProvider
            });
        }
    }
}
