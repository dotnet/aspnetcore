// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Mime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for mapping Blazor WebAssembly applications.
    /// </summary>
    public static class ComponentsWebAssemblyEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps a Blazor webassembly application to the <paramref name="pathPrefix"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="pathPrefix">The <see cref="PathString"/> that indicates the prefix for the Blazor application.</param>
        /// <returns>The <see cref="IEndpointConventionBuilder"/></returns>
        public static IEndpointConventionBuilder MapBlazorWebAssemblyApplication(this IEndpointRouteBuilder endpoints, PathString pathPrefix)
        {
            if (endpoints is null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var webHostEnvironment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            var options = CreateStaticFilesOptions(webHostEnvironment.WebRootFileProvider);
            var appBuilder = endpoints.CreateApplicationBuilder();

            appBuilder.Use(async (ctx, next) =>
            {
                var endpoint = ctx.GetEndpoint();
                try
                {
                    // Set the endpoint to null so that static files doesn't discard the path.
                    ctx.SetEndpoint(null);

                    if (ctx.Request.Path.StartsWithSegments(pathPrefix, out var rest) &&
                        rest.StartsWithSegments("/_framework"))
                    {
                        // At this point we mapped something from the /_framework
                        ctx.Response.Headers.Append(HeaderNames.CacheControl, "no-cache");
                    }

                    // This will invoke the static files middleware plugged-in below.
                    await next();

                }
                finally
                {
                    ctx.SetEndpoint(endpoint);
                }
            });

            appBuilder.UseStaticFiles(options);

            var conventionBuilder = endpoints.Map(
                $"{pathPrefix}/{{*path:file}}",
                appBuilder.Build());

            conventionBuilder.Add(builder =>
            {
                // Map this route with low priority so that it doesn't interfere with any other potential request.
                ((RouteEndpointBuilder)builder).Order = int.MaxValue - 100;
            });

            return conventionBuilder;
        }

        /// <summary>
        /// Maps a Blazor webassembly application to the root path of the application "/".
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="pathPrefix">The <see cref="PathString"/> that indicates the prefix for the Blazor application.</param>
        /// <returns>The <see cref="IEndpointConventionBuilder"/></returns>
        public static IEndpointConventionBuilder MapBlazorWebAssemblyApplication(this IEndpointRouteBuilder endpoints) =>
            MapBlazorWebAssemblyApplication(endpoints, default);

        private static StaticFileOptions CreateStaticFilesOptions(IFileProvider webRootFileProvider)
        {
            var options = new StaticFileOptions();
            options.FileProvider = webRootFileProvider;
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            AddMapping(contentTypeProvider, ".dll", MediaTypeNames.Application.Octet);
            // We unconditionally map pdbs as there will be no pdbs in the output folder for
            // release builds unless BlazorEnableDebugging is explicitly set to true.
            AddMapping(contentTypeProvider, ".pdb", MediaTypeNames.Application.Octet);

            options.ContentTypeProvider = contentTypeProvider;

            return options;
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
