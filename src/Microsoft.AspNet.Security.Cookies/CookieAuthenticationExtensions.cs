// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Security.Cookies;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods provided by the cookies authentication middleware
    /// </summary>
    public static class CookieAuthenticationExtensions
    {
        /// <summary>
        /// Adds a cookie-based authentication middleware to your web application pipeline.
        /// </summary>
        /// <param name="app">The IBuilder passed to your configuration method</param>
        /// <param name="options">An options class that controls the middleware behavior</param>
        /// <returns>The original app parameter</returns>
        public static IBuilder UseCookieAuthentication([NotNull] this IBuilder app, [NotNull] CookieAuthenticationOptions options)
        {
            return app.UseMiddleware<CookieAuthenticationMiddleware>(options);
        }
    }
}