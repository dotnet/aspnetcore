// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.ResponseCaching;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for adding the <see cref="ResponseCachingMiddleware"/> to an application.
    /// </summary>
    public static class ResponseCachingExtensions
    {
        /// <summary>
        /// Adds the <see cref="ResponseCachingMiddleware"/> for caching HTTP responses.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        public static IApplicationBuilder UseResponseCaching(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ResponseCachingMiddleware>();
        }
    }
}
