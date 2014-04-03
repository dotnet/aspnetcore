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

            // TODO: Extension methods for this?
            var loggerFactory = (ILoggerFactory)app.ServiceProvider.GetService(typeof(ILoggerFactory)) ?? new NullLoggerFactory();
            ILogger logger = loggerFactory.Create(typeof(CookieAuthenticationMiddleware).FullName);

            if (options.TicketDataFormat == null)
            {
                IDataProtector dataProtector = app.CreateDataProtector(
                    typeof(CookieAuthenticationMiddleware).FullName,
                    options.AuthenticationType, "v1");
                options.TicketDataFormat = new TicketDataFormat(dataProtector);
            }

            return app.Use(next => new CookieAuthenticationMiddleware(next, logger, options).Invoke);
        }

        // TODO: Temp workaround until the host reliably provides logging.
        private class NullLoggerFactory : ILoggerFactory
        {
            public ILogger Create(string name)
            {
                return new NullLongger();
            }
        }

        private class NullLongger : ILogger
        {
            public bool WriteCore(TraceType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                return false;
            }
        }
    }
}