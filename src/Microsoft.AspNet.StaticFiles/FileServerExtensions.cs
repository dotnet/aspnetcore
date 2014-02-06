// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.StaticFiles;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet
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
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IBuilder UseFileServer(this IBuilder builder)
        {
            return UseFileServer(builder, new FileServerOptions());
        }

        /// <summary>
        /// Enable all static file middleware on for the current request path in the current directory.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="enableDirectoryBrowsing">Should directory browsing be enabled?</param>
        /// <returns></returns>
        public static IBuilder UseFileServer(this IBuilder builder, bool enableDirectoryBrowsing)
        {
            return UseFileServer(builder, new FileServerOptions() { EnableDirectoryBrowsing = enableDirectoryBrowsing });
        }

        /// <summary>
        /// Enables all static file middleware (except directory browsing) for the given request path from the directory of the same name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path and physical path.</param>
        /// <returns></returns>
        public static IBuilder UseFileServer(this IBuilder builder, string requestPath)
        {
            return UseFileServer(builder, new FileServerOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enable all static file middleware with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IBuilder UseFileServer(this IBuilder builder, FileServerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (options.EnableDefaultFiles)
            {
                builder = builder.UseDefaultFiles(options.DefaultFilesOptions);
            }

            if (options.EnableDirectoryBrowsing)
            {
                builder = builder.UseDirectoryBrowser(options.DirectoryBrowserOptions);
            }

            return builder
                .UseSendFileFallback()
                .UseStaticFiles(options.StaticFileOptions);
        }
    }
}