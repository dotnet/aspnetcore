// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;

namespace Microsoft.AspNetCore.Blazor.Cli.Server
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddResponseCompression(options =>
            {
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                    MediaTypeNames.Application.Octet,
                    WasmMediaTypeNames.Application.Wasm
                });
            });
        }

        public void Configure(IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseDeveloperExceptionPage();
            app.UseResponseCompression();
            EnableConfiguredPathbase(app, configuration);

            var clientAssemblyPath = FindClientAssemblyPath(app);
            app.UseBlazor(new BlazorOptions { ClientAssemblyPath = clientAssemblyPath });
        }

        private static void EnableConfiguredPathbase(IApplicationBuilder app, IConfiguration configuration)
        {
            var pathBase = configuration.GetValue<string>("pathbase");
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);

                // To ensure consistency with a production environment, only handle requests
                // that match the specified pathbase.
                app.Use((context, next) =>
                {
                    if (context.Request.PathBase == pathBase)
                    {
                        return next();
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        return context.Response.WriteAsync($"The server is configured only to " +
                            $"handle request URIs within the PathBase '{pathBase}'.");
                    }
                });
            }
        }

        private static string FindClientAssemblyPath(IApplicationBuilder app)
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

            return assemblyPath;
        }

        private static string FindClientBinDir(string clientAppSourceRoot)
        {
            // As a temporary workaround for https://github.com/aspnet/Blazor/issues/261,
            // disallow the scenario where there is both a Debug *and* a Release dir.
            // Only allow there to be one, and that's the one we pick.
            var debugDirPath = Path.Combine(clientAppSourceRoot, "bin", "Debug");
            var releaseDirPath = Path.Combine(clientAppSourceRoot, "bin", "Release");
            var debugDirExists = Directory.Exists(debugDirPath);
            var releaseDirExists = Directory.Exists(releaseDirPath);
            if (debugDirExists && releaseDirExists)
            {
                throw new InvalidOperationException($"Cannot identify unique bin directory for Blazor app. " +
                    $"Found both '{debugDirPath}' and '{releaseDirPath}'. Ensure that only one is present on " +
                    $"disk. This is a temporary limitation (see https://github.com/aspnet/Blazor/issues/261).");
            }

            if (!(debugDirExists || releaseDirExists))
            {
                throw new InvalidOperationException($"Cannot find bin directory for Blazor app. " +
                    $"Neither '{debugDirPath}' nor '{releaseDirPath}' exists on disk. Make sure the project has been built.");
            }

            var binDir = debugDirExists ? debugDirPath : releaseDirPath;

            var subdirectories = Directory.GetDirectories(binDir);
            if (subdirectories.Length != 1)
            {
                throw new InvalidOperationException($"Could not locate bin directory for Blazor app. " +
                    $"Expected to find exactly 1 subdirectory in '{binDir}', but found {subdirectories.Length}.");
            }

            return Path.Combine(binDir, subdirectories[0]);
        }
    }
}
