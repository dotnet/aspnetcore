// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.StaticFiles;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for the DirectoryBrowserMiddleware
    /// </summary>
    public static class DirectoryBrowserExtensions
    {
        /// <summary>
        /// Enable directory browsing on the current path
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDirectoryBrowser(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseDirectoryBrowser(new DirectoryBrowserOptions());
        }

        /// <summary>
        /// Enables directory browsing for the given request path
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseDirectoryBrowser(this IApplicationBuilder builder, string requestPath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseDirectoryBrowser(new DirectoryBrowserOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enable directory browsing with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDirectoryBrowser(this IApplicationBuilder builder, DirectoryBrowserOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.UseMiddleware<DirectoryBrowserMiddleware>(options);
        }
    }
}