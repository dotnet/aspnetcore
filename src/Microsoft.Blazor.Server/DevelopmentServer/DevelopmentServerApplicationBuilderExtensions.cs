// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;

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
            ServeClientBinDir(applicationBuilder, sourcePath);
        }

        private static void ServeClientBinDir(IApplicationBuilder applicationBuilder, string clientAppSourceRoot)
        {
            var clientBinDirPath = FindClientBinDir(clientAppSourceRoot);
            applicationBuilder.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = "/_bin",
                FileProvider = new PhysicalFileProvider(clientBinDirPath),
                ContentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>
                {
                    { ".dll", MediaTypeNames.Application.Octet },
                })
            });
        }

        private static string FindClientBinDir(string clientAppSourceRoot)
        {
            var binDebugDir = Path.Combine(clientAppSourceRoot, "bin", "Debug");
            var subdirectories = Directory.GetDirectories(binDebugDir);
            if (subdirectories.Length != 1)
            {
                throw new InvalidOperationException($"Could not locate bin directory for Blazor app. " +
                    $"Expected to find exactly 1 subdirectory in '{binDebugDir}', but found {subdirectories.Length}.");
            }

            return Path.Combine(binDebugDir, subdirectories[0]);
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
