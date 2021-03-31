// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the <see cref="RewriteMiddleware"/>
    /// </summary>
    public static class RewriteBuilderExtensions
    {
        /// <summary>
        /// Checks if a given Url matches rules and conditions, and modifies the HttpContext on match.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRewriter(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<RewriteMiddleware>();
        }

        /// <summary>
        /// Checks if a given Url matches rules and conditions, and modifies the HttpContext on match.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/></param>
        /// <param name="options">Options for rewrite.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseRewriter(this IApplicationBuilder app, RewriteOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // put middleware in pipeline
            return app.UseMiddleware<RewriteMiddleware>(Options.Create(options));
        }
    }
}
