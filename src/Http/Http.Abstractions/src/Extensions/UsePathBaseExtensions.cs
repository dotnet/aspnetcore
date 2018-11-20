// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder.Extensions;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class UsePathBaseExtensions
    {
        /// <summary>
        /// Adds a middleware that extracts the specified path base from request path and postpend it to the request path base.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <param name="pathBase">The path base to extract.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder UsePathBase(this IApplicationBuilder app, PathString pathBase)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            // Strip trailing slashes
            pathBase = pathBase.Value?.TrimEnd('/');
            if (!pathBase.HasValue)
            {
                return app;
            }

            return app.UseMiddleware<UsePathBaseMiddleware>(pathBase);
        }
    }
}