// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the <see cref="RewriteMiddleware"/> 
    /// </summary>
    public static class RewriteExtensions
    {
        /// <summary>
        /// Checks if a given Url matches rules and conditions, and modifies the HttpContext on match.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options">Options for urlrewrite.</param>
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
            return app.UseMiddleware<RewriteMiddleware>(options);
        }
    }
}
