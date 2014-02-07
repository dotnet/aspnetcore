// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.StaticFiles;

namespace Microsoft.AspNet
{
    /// <summary>
    /// Extension methods for the DefaultFilesMiddleware
    /// </summary>
    public static class DefaultFilesExtensions
    {
        /// <summary>
        /// Enables default file mapping on the current path from the current directory
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IBuilder UseDefaultFiles(this IBuilder builder)
        {
            return builder.UseDefaultFiles(new DefaultFilesOptions());
        }

        /// <summary>
        /// Enables default file mapping for the given request path from the directory of the same name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path and physical path.</param>
        /// <returns></returns>
        public static IBuilder UseDefaultFiles(this IBuilder builder, string requestPath)
        {
            return UseDefaultFiles(builder, new DefaultFilesOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enables default file mapping with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IBuilder UseDefaultFiles(this IBuilder builder, DefaultFilesOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            return builder.Use(next => new DefaultFilesMiddleware(next, options).Invoke);
        }
    }
}