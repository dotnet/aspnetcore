// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HttpLogging;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the HttpLogging middleware.
    /// </summary>
    public static class HttpLoggingBuilderExtensions
    {
        /// <summary>
        /// Adds middleware for HTTP Logging.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for HttpLogging.</returns>
        public static IApplicationBuilder UseHttpLogging(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<HttpLoggingMiddleware>();
            return app;
        }
    }
}
