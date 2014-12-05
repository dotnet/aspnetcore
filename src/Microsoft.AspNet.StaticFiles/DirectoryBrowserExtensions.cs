// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public static IApplicationBuilder UseDirectoryBrowser([NotNull] this IApplicationBuilder builder)
        {
            return builder.UseDirectoryBrowser(new DirectoryBrowserOptions());
        }

        /// <summary>
        /// Enables directory browsing for the given request path
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseDirectoryBrowser([NotNull] this IApplicationBuilder builder, [NotNull] string requestPath)
        {
            return builder.UseDirectoryBrowser(new DirectoryBrowserOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enable directory browsing with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDirectoryBrowser([NotNull] this IApplicationBuilder builder, [NotNull] DirectoryBrowserOptions options)
        {
            return builder.UseMiddleware<DirectoryBrowserMiddleware>(options);
        }
    }
}