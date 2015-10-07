// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.StaticFiles;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for the DefaultFilesMiddleware
    /// </summary>
    public static class DefaultFilesExtensions
    {
        /// <summary>
        /// Enables default file mapping on the current path
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseDefaultFiles(new DefaultFilesOptions());
        }

        /// <summary>
        /// Enables default file mapping for the given request path
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder builder, string requestPath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseDefaultFiles(new DefaultFilesOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enables default file mapping with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder builder, DefaultFilesOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.UseMiddleware<DefaultFilesMiddleware>(options);
        }
    }
}