// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// IApplicationBuilder extensions for the WelcomePageMiddleware.
    /// </summary>
    public static class WelcomePageExtensions
    {
        /// <summary>
        /// Adds the WelcomePageMiddleware to the pipeline with the given options.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app, WelcomePageOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<WelcomePageMiddleware>(Options.Create(options));
        }

        /// <summary>
        /// Adds the WelcomePageMiddleware to the pipeline with the given path.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app, PathString path)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseWelcomePage(new WelcomePageOptions
            {
                Path = path
            });
        }

        /// <summary>
        /// Adds the WelcomePageMiddleware to the pipeline with the given path.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app, string path)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseWelcomePage(new WelcomePageOptions
            {
                Path = new PathString(path)
            });
        }

        /// <summary>
        /// Adds the WelcomePageMiddleware to the pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<WelcomePageMiddleware>();
        }
    }
}