// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Blazor.DevServer.Server
{
    internal class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.AddResponseCompression(options =>
            {
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                    MediaTypeNames.Application.Octet,
                    "application/wasm",
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment, IConfiguration configuration)
        {
            var applicationAssemblyFullPath = ResolveApplicationAssemblyFullPath();

            app.UseDeveloperExceptionPage();
            app.UseResponseCompression();
            EnableConfiguredPathbase(app, configuration);

            app.UseBlazorDebugging();

            app.UseStaticFiles();
            app.UseClientSideBlazorFiles(applicationAssemblyFullPath);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapFallbackToClientSideBlazor(applicationAssemblyFullPath, "index.html");
            });
        }

        private string ResolveApplicationAssemblyFullPath()
        {
            const string applicationPathKey = "applicationpath";
            var configuredApplicationPath = Configuration.GetValue<string>(applicationPathKey);
            if (string.IsNullOrEmpty(configuredApplicationPath))
            {
                throw new InvalidOperationException($"No value was supplied for the required option '{applicationPathKey}'.");
            }

            var resolvedApplicationPath = Path.GetFullPath(configuredApplicationPath);
            if (!File.Exists(resolvedApplicationPath))
            {
                throw new InvalidOperationException($"Application assembly not found at {resolvedApplicationPath}.");
            }

            return resolvedApplicationPath;
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
    }
}
