// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.StaticFiles;

namespace Microsoft.AspNet.Builder
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

            return app.UseFileServer(options => { });
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

            return app.UseFileServer(options => { options.EnableDirectoryBrowsing = enableDirectoryBrowsing; });
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

            return app.UseFileServer(options => { options.RequestPath = new PathString(requestPath); });
        }

        /// <summary>
        /// Enable all static file middleware with the given options
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, Action<FileServerOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var fileServerOptions = new FileServerOptions();
            configureOptions(fileServerOptions);

            if (fileServerOptions.EnableDefaultFiles)
            {
                app = app.UseDefaultFiles(options => { options = fileServerOptions.DefaultFilesOptions; });
            }

            if (fileServerOptions.EnableDirectoryBrowsing)
            {
                app = app.UseDirectoryBrowser(options => { options = fileServerOptions.DirectoryBrowserOptions; });
            }

            return app.UseStaticFiles(options => { options = fileServerOptions.StaticFileOptions; });
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
                app = app.UseDefaultFiles(options.DefaultFilesOptions);
            }

            if (options.EnableDirectoryBrowsing)
            {
                app = app.UseDirectoryBrowser(options.DirectoryBrowserOptions);
            }

            return app.UseStaticFiles(options.StaticFileOptions);
        }
    }
}