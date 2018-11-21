// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods that combine all of the static file middleware components:
    /// Default files, directory browsing, send file, and static files
    /// </summary>
    public static class FileServerExtensions
    {
        /// <summary>
        /// Enable all static file middleware (except directory browsing) for the current request path in the current directory.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseFileServer(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseFileServer(new FileServerOptions());
        }

        /// <summary>
        /// Enable all static file middleware on for the current request path in the current directory.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="enableDirectoryBrowsing">Should directory browsing be enabled?</param>
        /// <returns></returns>
        public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, bool enableDirectoryBrowsing)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseFileServer(new FileServerOptions
            {
                EnableDirectoryBrowsing = enableDirectoryBrowsing
            });
        }

        /// <summary>
        /// Enables all static file middleware (except directory browsing) for the given request path from the directory of the same name
        /// </summary>
        /// <param name="app"></param>
        /// <param name="requestPath">The relative request path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, string requestPath)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (requestPath == null)
            {
                throw new ArgumentNullException(nameof(requestPath));
            }

            return app.UseFileServer(new FileServerOptions
            {
                RequestPath = new PathString(requestPath)
            });
        }

        /// <summary>
        /// Enable all static file middleware with the given options
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, FileServerOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.EnableDefaultFiles)
            {
                app.UseDefaultFiles(options.DefaultFilesOptions);
            }

            if (options.EnableDirectoryBrowsing)
            {
                app.UseDirectoryBrowser(options.DirectoryBrowserOptions);
            }

            return app.UseStaticFiles(options.StaticFileOptions);
        }
    }
}