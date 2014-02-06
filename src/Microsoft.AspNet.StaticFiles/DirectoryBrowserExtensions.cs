// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.StaticFiles;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet
{
    /// <summary>
    /// Extension methods for the DirectoryBrowserMiddleware
    /// </summary>
    public static class DirectoryBrowserExtensions
    {
        /// <summary>
        /// Enable directory browsing on the current path for the current directory
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IBuilder UseDirectoryBrowser(this IBuilder builder)
        {
            return builder.UseDirectoryBrowser(new DirectoryBrowserOptions());
        }

        /// <summary>
        /// Enables directory browsing for the given request path from the directory of the same name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path and physical path.</param>
        /// <returns></returns>
        public static IBuilder UseDirectoryBrowser(this IBuilder builder, string requestPath)
        {
            return UseDirectoryBrowser(builder, new DirectoryBrowserOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enable directory browsing with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IBuilder UseDirectoryBrowser(this IBuilder builder, DirectoryBrowserOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            return builder.Use(next => new DirectoryBrowserMiddleware(next, options).Invoke);
        }
    }
}