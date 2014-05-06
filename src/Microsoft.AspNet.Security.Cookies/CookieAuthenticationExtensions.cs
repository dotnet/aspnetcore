// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

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
        /// <param name="app">The IAppBuilder passed to your configuration method</param>
        /// <param name="options">An options class that controls the middleware behavior</param>
        /// <returns>The original app parameter</returns>
        public static IBuilder UseCookieAuthentication([NotNull] this IBuilder app, [NotNull] CookieAuthenticationOptions options)
        {
            // TODO: Use UseMiddleware to inject dependencies once it can discover Invoke from a base class.
            var dataProtectionProvider = app.ApplicationServices.GetService<IDataProtectionProvider>();
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            return app.Use(next => new CookieAuthenticationMiddleware(next, dataProtectionProvider, loggerFactory, options).Invoke);
        }
    }
}