// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for hosting client-side Blazor applications in ASP.NET Core.
    /// </summary>
    public static class BlazorHostingEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds a low-priority endpoint that will serve the the file specified by <paramref name="filePath"/> from the client-side
        /// Blazor application specified by <typeparamref name="TClientApp"/>.
        /// </summary>
        /// <typeparam name="TClientApp">A type in the client-side application.</typeparam>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="filePath">
        /// The relative path to the entry point of the client-side application. The path is relative to the
        /// <see cref="IWebHostEnvironment.WebRootPath"/>, commonly <c>wwwroot</c>.
        /// </param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method is intended to handle cases where URL path of the request does not contain a filename, and no other
        /// endpoint has matched. This is convenient for routing requests for dynamic content to the client-side blazor
        /// application, while also allowing requests for non-existent files to result in an HTTP 404.
        /// </para>
        /// </remarks>
        public static IEndpointConventionBuilder MapFallbackToClientSideBlazor<TClientApp>(this IEndpointRouteBuilder endpoints, string filePath)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return MapFallbackToClientSideBlazor(endpoints, typeof(TClientApp).Assembly.Location, FallbackEndpointRouteBuilderExtensions.DefaultPattern, filePath);
        }

        /// <summary>
        /// Adds a low-priority endpoint that will serve the the file specified by <paramref name="filePath"/> from the client-side
        /// Blazor application specified by <paramref name="clientAssemblyFilePath"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="clientAssemblyFilePath">The file path of the client-side Blazor application assembly.</param>
        /// <param name="filePath">
        /// The relative path to the entry point of the client-side application. The path is relative to the
        /// <see cref="IWebHostEnvironment.WebRootPath"/>, commonly <c>wwwroot</c>.
        /// </param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method is intended to handle cases where URL path of the request does not contain a filename, and no other
        /// endpoint has matched. This is convenient for routing requests for dynamic content to the client-side blazor
        /// application, while also allowing requests for non-existent files to result in an HTTP 404.
        /// </para>
        /// </remarks>
        public static IEndpointConventionBuilder MapFallbackToClientSideBlazor(this IEndpointRouteBuilder endpoints, string clientAssemblyFilePath, string filePath)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (clientAssemblyFilePath == null)
            {
                throw new ArgumentNullException(nameof(clientAssemblyFilePath));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return MapFallbackToClientSideBlazor(endpoints, clientAssemblyFilePath, FallbackEndpointRouteBuilderExtensions.DefaultPattern, filePath);
        }

        /// <summary>
        /// Adds a low-priority endpoint that will serve the the file specified by <paramref name="filePath"/> from the client-side
        /// Blazor application specified by <typeparamref name="TClientApp"/>.
        /// </summary>
        /// <typeparam name="TClientApp">A type in the client-side application.</typeparam>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="pattern">The route pattern to match.</param>
        /// <param name="filePath">
        /// The relative path to the entry point of the client-side application. The path is relative to the
        /// <see cref="IWebHostEnvironment.WebRootPath"/>, commonly <c>wwwroot</c>.
        /// </param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method is intended to handle cases where URL path of the request does not contain a filename, and no other
        /// endpoint has matched. This is convenient for routing requests for dynamic content to the client-side blazor
        /// application, while also allowing requests for non-existent files to result in an HTTP 404.
        /// </para>
        /// </remarks>
        public static IEndpointConventionBuilder MapFallbackToClientSideBlazor<TClientApp>(this IEndpointRouteBuilder endpoints, string pattern, string filePath)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return MapFallbackToClientSideBlazor(endpoints, typeof(TClientApp).Assembly.Location, pattern, filePath);
        }

        /// <summary>
        /// Adds a low-priority endpoint that will serve the the file specified by <paramref name="filePath"/> from the client-side
        /// Blazor application specified by <paramref name="clientAssemblyFilePath"/>.
        /// </summary>
        /// <param name="clientAssemblyFilePath">The file path of the client-side Blazor application assembly.</param>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="pattern">The route pattern to match.</param>
        /// <param name="filePath">
        /// The relative path to the entry point of the client-side application. The path is relative to the
        /// <see cref="IWebHostEnvironment.WebRootPath"/>, commonly <c>wwwroot</c>.
        /// </param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method is intended to handle cases where URL path of the request does not contain a filename, and no other
        /// endpoint has matched. This is convenient for routing requests for dynamic content to the client-side blazor
        /// application, while also allowing requests for non-existent files to result in an HTTP 404.
        /// </para>
        /// </remarks>
        public static IEndpointConventionBuilder MapFallbackToClientSideBlazor(this IEndpointRouteBuilder endpoints, string clientAssemblyFilePath, string pattern, string filePath)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (clientAssemblyFilePath == null)
            {
                throw new ArgumentNullException(nameof(clientAssemblyFilePath));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var config = BlazorConfig.Read(clientAssemblyFilePath);

            // We want to serve "index.html" from whichever directory contains it in this priority order:
            // 1. Client app "dist" directory
            // 2. Client app "wwwroot" directory
            // 3. Server app "wwwroot" directory
            var directory = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>().WebRootPath;
            var indexHtml = config.FindIndexHtmlFile();
            if (indexHtml != null)
            {
                directory = Path.GetDirectoryName(indexHtml);
            }

            var options = new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(directory),
                OnPrepareResponse = CacheHeaderSettings.SetCacheHeaders,
            };

            return endpoints.MapFallbackToFile(pattern, filePath, options);
        }
    }
}
