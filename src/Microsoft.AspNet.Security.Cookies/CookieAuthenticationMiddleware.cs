// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Logging;
using Microsoft.AspNet.Security.DataHandler;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.AspNet.Security.Infrastructure;

namespace Microsoft.AspNet.Security.Cookies
{
    internal class CookieAuthenticationMiddleware : AuthenticationMiddleware<CookieAuthenticationOptions>
    {
        private readonly ILogger _logger;

        public CookieAuthenticationMiddleware(RequestDelegate next, IDataProtectionProvider dataProtectionProvider, ILoggerFactory loggerFactory, CookieAuthenticationOptions options)
            : base(next, options)
        {
            if (Options.Notifications == null)
            {
                Options.Notifications = new CookieAuthenticationNotifications();
            }
            if (String.IsNullOrEmpty(Options.CookieName))
            {
                Options.CookieName = CookieAuthenticationDefaults.CookiePrefix + Options.AuthenticationType;
            }
            if (options.TicketDataFormat == null)
            {
                IDataProtector dataProtector = DataProtectionHelpers.CreateDataProtector(dataProtectionProvider,
                    typeof(CookieAuthenticationMiddleware).FullName, options.AuthenticationType, "v1");
                options.TicketDataFormat = new TicketDataFormat(dataProtector);
            }

            _logger = loggerFactory.Create(typeof(CookieAuthenticationMiddleware).FullName);
        }

        protected override AuthenticationHandler<CookieAuthenticationOptions> CreateHandler()
        {
            return new CookieAuthenticationHandler(_logger);
        }
    }
}