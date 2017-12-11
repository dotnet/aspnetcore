// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Microsoft.Blazor.DevHost.Server
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseBlazorDevelopmentServer(".");
            app.UseBlazor(clientAssembly: FindClientAssembly(app));
        }

        private static Assembly FindClientAssembly(IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            var contentRoot = env.ContentRootPath;
            var binDir = FindClientBinDir(contentRoot);
            var appName = Path.GetFileName(contentRoot); // TODO: Allow for the possibility that the assembly name has been overridden
            var assemblyPath = Path.Combine(binDir, $"{appName}.dll");
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Could not locate application assembly at expected location {assemblyPath}");
            }

            return Assembly.LoadFile(assemblyPath);
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
    }
}
