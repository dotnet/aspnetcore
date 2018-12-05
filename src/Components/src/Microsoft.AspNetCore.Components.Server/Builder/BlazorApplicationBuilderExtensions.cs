// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;
using System;
using System.IO;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods that add Blazor-related middleware to the ASP.NET pipeline.
    /// </summary>
    public static class BlazorApplicationBuilderExtensions
    {
        const string DevServerApplicationName = "dotnet-blazor";

        /// <summary>
        /// Configures the middleware pipeline to work with Blazor.
        /// </summary>
        /// <typeparam name="TProgram">Any type from the client app project. This is used to identify the client app assembly.</typeparam>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseBlazor<TProgram>(
            this IApplicationBuilder app)
        {
            var clientAssemblyInServerBinDir = typeof(TProgram).Assembly;
            return app.UseBlazor(new BlazorOptions
            {
                ClientAssemblyPath = clientAssemblyInServerBinDir.Location,
            });
        }

        /// <summary>
        /// Configures the middleware pipeline to work with Blazor.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="options">Options to configure the middleware.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseBlazor(
            this IApplicationBuilder app,
            BlazorOptions options)
        {
            // TODO: Make the .blazor.config file contents sane
            // Currently the items in it are bizarre and don't relate to their purpose,
            // hence all the path manipulation here. We shouldn't be hardcoding 'dist' here either.
            var env = (IHostingEnvironment)app.ApplicationServices.GetService(typeof(IHostingEnvironment));
            var config = BlazorConfig.Read(options.ClientAssemblyPath);

            if (env.IsDevelopment() && config.EnableAutoRebuilding)
            {
                if (env.ApplicationName.Equals(DevServerApplicationName, StringComparison.OrdinalIgnoreCase))
                {
                    app.UseDevServerAutoRebuild(config);
                }
                else
                {
                    app.UseHostedAutoRebuild(config, env.ContentRootPath);
                }
            }

            // First, match the request against files in the client app dist directory
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(config.DistPath),
                ContentTypeProvider = CreateContentTypeProvider(config.EnableDebugging),
                OnPrepareResponse = SetCacheHeaders
            });

            // * Before publishing, we serve the wwwroot files directly from source
            //   (and don't require them to be copied into dist).
            //   In this case, WebRootPath will be nonempty if that directory exists.
            // * After publishing, the wwwroot files are already copied to 'dist' and
            //   will be served by the above middleware, so we do nothing here.
            //   In this case, WebRootPath will be empty (the publish process sets this).
            if (!string.IsNullOrEmpty(config.WebRootPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(config.WebRootPath),
                    OnPrepareResponse = SetCacheHeaders
                });
            }

            // Accept debugger connections
            if (config.EnableDebugging)
            {
                app.UseMonoDebugProxy();
            }

            // Finally, use SPA fallback routing (serve default page for anything else,
            // excluding /_framework/*)
            app.MapWhen(IsNotFrameworkDir, childAppBuilder =>
            {
                var indexHtmlPath = FindIndexHtmlFile(config);
                var indexHtmlStaticFileOptions = string.IsNullOrEmpty(indexHtmlPath)
                    ? null : new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(indexHtmlPath)),
                        OnPrepareResponse = SetCacheHeaders
                    };

                childAppBuilder.UseSpa(spa =>
                {
                    spa.Options.DefaultPageStaticFileOptions = indexHtmlStaticFileOptions;
                });
            });

            return app;
        }

        private static string FindIndexHtmlFile(BlazorConfig config)
        {
            // Before publishing, the client project may have a wwwroot directory.
            // If so, and if it contains index.html, use that.
            if (!string.IsNullOrEmpty(config.WebRootPath))
            {
                var wwwrootIndexHtmlPath = Path.Combine(config.WebRootPath, "index.html");
                if (File.Exists(wwwrootIndexHtmlPath))
                {
                    return wwwrootIndexHtmlPath;
                }
            }

            // After publishing, the client project won't have a wwwroot directory.
            // The contents from that dir will have been copied to "dist" during publish.
            // So if "dist/index.html" now exists, use that.
            var distIndexHtmlPath = Path.Combine(config.DistPath, "index.html");
            if (File.Exists(distIndexHtmlPath))
            {
                return distIndexHtmlPath;
            }

            // Since there's no index.html, we'll use the default DefaultPageStaticFileOptions,
            // hence we'll look for index.html in the host server app's wwwroot.
            return null;
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

        private static IContentTypeProvider CreateContentTypeProvider(bool enableDebugging)
        {
            var result = new FileExtensionContentTypeProvider();
            AddMapping(result, ".dll", MediaTypeNames.Application.Octet);
            AddMapping(result, ".wasm", WasmMediaTypeNames.Application.Wasm);

            if (enableDebugging)
            {
                AddMapping(result, ".pdb", MediaTypeNames.Application.Octet);
            }

            return result;
        }

        private static void AddMapping(FileExtensionContentTypeProvider provider, string name, string mimeType)
        {
            if (!provider.Mappings.ContainsKey(name))
            {
                provider.Mappings.Add(name, mimeType);
            }
        }
    }
}
