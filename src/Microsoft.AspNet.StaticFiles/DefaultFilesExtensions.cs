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
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseDefaultFiles(options => { });
        }

        /// <summary>
        /// Enables default file mapping for the given request path
        /// </summary>
        /// <param name="app"></param>
        /// <param name="requestPath">The relative request path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder app, string requestPath)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseDefaultFiles(options => { options.RequestPath = new PathString(requestPath); });
        }

        /// <summary>
        /// Enables default file mapping with the given options
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder app, Action<DefaultFilesOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new DefaultFilesOptions();
            configureOptions(options);

            return app.UseMiddleware<DefaultFilesMiddleware>(options);
        }

        /// <summary>
        /// Enables default file mapping with the given options
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder app, DefaultFilesOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<DefaultFilesMiddleware>(options);
        }
    }
}