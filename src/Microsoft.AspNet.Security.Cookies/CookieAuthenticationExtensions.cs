// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Logging;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.Security.DataHandler;
using Microsoft.AspNet.Security.DataProtection;

namespace Microsoft.AspNet
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
        public static IBuilder UseCookieAuthentication(this IBuilder app, CookieAuthenticationOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            /*
            // TODO: Extension methods for this?
            var loggerFactory = (ILoggerFactory)app.ServiceProvider.GetService(typeof(ILoggerFactory));
            ILogger logger = loggerFactory.Create(typeof(CookieAuthenticationMiddleware).FullName);
            */
            ILogger logger = null;

            if (options.TicketDataFormat == null)
            {
                /* TODO: Add DPP extensions
                IDataProtector dataProtector = app.CreateDataProtector(
                    typeof(CookieAuthenticationMiddleware).FullName,
                    options.AuthenticationType, "v1");
                */
                var dataProtectionProvider = (IDataProtectionProvider)app.ServiceProvider.GetService(typeof(IDataProtectionProvider));
                IDataProtector dataProtector = dataProtectionProvider.CreateProtector(string.Join(";", typeof(CookieAuthenticationMiddleware).FullName, options.AuthenticationType, "v1"));
                options.TicketDataFormat = new TicketDataFormat(dataProtector);
            }

            app.Use(next => new CookieAuthenticationMiddleware(next, logger, options).Invoke);
            // TODO: ? app.UseStageMarker(PipelineStage.Authenticate);
            return app;
        }
    }
}