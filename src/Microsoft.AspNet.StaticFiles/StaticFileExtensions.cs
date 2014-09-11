// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.StaticFiles;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for the StaticFileMiddleware
    /// </summary>
    public static class StaticFileExtensions
    {
        /// <summary>
        /// Enables static file serving for the current request path from the current directory
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStaticFiles([NotNull] this IApplicationBuilder builder)
        {
            return builder.UseStaticFiles(new StaticFileOptions());
        }

        /// <summary>
        /// Enables static file serving for the given request path from the directory of the same name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path and physical path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseStaticFiles([NotNull] this IApplicationBuilder builder, [NotNull] string requestPath)
        {
            return builder.UseStaticFiles(new StaticFileOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enables static file serving with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStaticFiles([NotNull] this IApplicationBuilder builder, [NotNull] StaticFileOptions options)
        {
            return builder.UseMiddleware<StaticFileMiddleware>(options);
        }
    }
}
