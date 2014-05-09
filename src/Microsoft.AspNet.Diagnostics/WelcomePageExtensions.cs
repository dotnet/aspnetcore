// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Diagnostics;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// IBuilder extensions for the WelcomePageMiddleware.
    /// </summary>
    public static class WelcomePageExtensions
    {
        /// <summary>
        /// Adds the WelcomePageMiddleware to the pipeline with the given options.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IBuilder UseWelcomePage(this IBuilder builder, WelcomePageOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            return builder.Use(next => new WelcomePageMiddleware(next, options).Invoke);
        }

        /// <summary>
        /// Adds the WelcomePageMiddleware to the pipeline with the given path.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IBuilder UseWelcomePage(this IBuilder builder, PathString path)
        {
            return UseWelcomePage(builder, new WelcomePageOptions { Path = path });
        }

        /// <summary>
        /// Adds the WelcomePageMiddleware to the pipeline with the given path.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IBuilder UseWelcomePage(this IBuilder builder, string path)
        {
            return UseWelcomePage(builder, new WelcomePageOptions { Path = new PathString(path) });
        }

        /// <summary>
        /// Adds the WelcomePageMiddleware to the pipeline.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IBuilder UseWelcomePage(this IBuilder builder)
        {
            return UseWelcomePage(builder, new WelcomePageOptions());
        }
    }
}