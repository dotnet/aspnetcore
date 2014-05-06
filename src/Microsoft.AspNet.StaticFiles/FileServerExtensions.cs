// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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