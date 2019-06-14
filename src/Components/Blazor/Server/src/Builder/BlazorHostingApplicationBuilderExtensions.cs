// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for hosting client-side Blazor applications in ASP.NET Core.
    /// </summary>
    public static class BlazorHostingApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="StaticFileMiddleware"/> that will serve static files from the client-side Blazor application
        /// specified by <typeparamref name="TClientApp"/>.
        /// </summary>
        /// <typeparam name="TClientApp">A type in the client-side application.</typeparam>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseClientSideBlazorFiles<TClientApp>(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            UseClientSideBlazorFiles(app, typeof(TClientApp).Assembly.Location);
            return app;
        }

        /// <summary>
        /// Adds a <see cref="StaticFileMiddleware"/> that will serve static files from the client-side Blazor application
        /// specified by <paramref name="clientAssemblyFilePath"/>.
        /// </summary>
        /// <param name="clientAssemblyFilePath">The file path of the client-side Blazor application assembly.</param>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseClientSideBlazorFiles(this IApplicationBuilder app, string clientAssemblyFilePath)
        {
            if (clientAssemblyFilePath == null)
            {
                throw new ArgumentNullException(nameof(clientAssemblyFilePath));
            }

            var fileProviders = new List<IFileProvider>();

            // TODO: Make the .blazor.config file contents sane
            // Currently the items in it are bizarre and don't relate to their purpose,
            // hence all the path manipulation here. We shouldn't be hardcoding 'dist' here either.
            var config = BlazorConfig.Read(clientAssemblyFilePath);

            // First, match the request against files in the client app dist directory
            fileProviders.Add(new PhysicalFileProvider(config.DistPath));

            // * Before publishing, we serve the wwwroot files directly from source
            //   (and don't require them to be copied into dist).
            //   In this case, WebRootPath will be nonempty if that directory exists.
            // * After publishing, the wwwroot files are already copied to 'dist' and
            //   will be served by the above middleware, so we do nothing here.
            //   In this case, WebRootPath will be empty (the publish process sets this).
            if (!string.IsNullOrEmpty(config.WebRootPath))
            {
                fileProviders.Add(new PhysicalFileProvider(config.WebRootPath));
            }

            // We can't modify an IFileContentTypeProvider, so we have to decorate.
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            AddMapping(contentTypeProvider, ".dll", MediaTypeNames.Application.Octet);
            if (config.EnableDebugging)
            {
                AddMapping(contentTypeProvider, ".pdb", MediaTypeNames.Application.Octet);
            }

            var options = new StaticFileOptions()
            {
                ContentTypeProvider = contentTypeProvider,
                FileProvider = new CompositeFileProvider(fileProviders),
                OnPrepareResponse = CacheHeaderSettings.SetCacheHeaders,
            };

            app.UseStaticFiles(options);
            return app;

            static void AddMapping(FileExtensionContentTypeProvider provider, string name, string mimeType)
            {
                if (!provider.Mappings.ContainsKey(name))
                {
                    provider.Mappings.Add(name, mimeType);
                }
            }
        }
    }
}
