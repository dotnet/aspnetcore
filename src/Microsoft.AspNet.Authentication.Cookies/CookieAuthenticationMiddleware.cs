// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Cookies.Infrastructure;
using Microsoft.AspNet.Authentication.DataHandler;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Authentication.Cookies
{
    public class CookieAuthenticationMiddleware : AuthenticationMiddleware<CookieAuthenticationOptions>
    {
        private readonly ILogger _logger;

        public CookieAuthenticationMiddleware(RequestDelegate next, 
            IServiceProvider services,
            IDataProtectionProvider dataProtectionProvider, 
            ILoggerFactory loggerFactory, 
            IOptions<CookieAuthenticationOptions> options,
            ConfigureOptions<CookieAuthenticationOptions> configureOptions)
            : base(next, services, options, configureOptions)
        {
            if (Options.Notifications == null)
            {
                Options.Notifications = new CookieAuthenticationNotifications();
            }
            if (String.IsNullOrEmpty(Options.CookieName))
            {
                Options.CookieName = CookieAuthenticationDefaults.CookiePrefix + Options.AuthenticationScheme;
            }
            if (Options.TicketDataFormat == null)
            {
                IDataProtector dataProtector = dataProtectionProvider.CreateProtector(
                    typeof(CookieAuthenticationMiddleware).FullName, Options.AuthenticationScheme, "v2");
                Options.TicketDataFormat = new TicketDataFormat(dataProtector);
            }
            if (Options.CookieManager == null)
            {
                Options.CookieManager = new ChunkingCookieManager();
            }

            _logger = loggerFactory.CreateLogger(typeof(CookieAuthenticationMiddleware).FullName);
        }

        protected override AuthenticationHandler<CookieAuthenticationOptions> CreateHandler()
        {
            return new CookieAuthenticationHandler(_logger);
        }
    }
}