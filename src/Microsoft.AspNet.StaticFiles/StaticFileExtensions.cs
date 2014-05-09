// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        public static IBuilder UseStaticFiles(this IBuilder builder)
        {
            return UseStaticFiles(builder, new StaticFileOptions());
        }

        /// <summary>
        /// Enables static file serving for the given request path from the directory of the same name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path and physical path.</param>
        /// <returns></returns>
        public static IBuilder UseStaticFiles(this IBuilder builder, string requestPath)
        {
            return UseStaticFiles(builder, new StaticFileOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enables static file serving with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IBuilder UseStaticFiles(this IBuilder builder, StaticFileOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            return builder.Use(next => new StaticFileMiddleware(next, options).Invoke);
        }
    }
}
