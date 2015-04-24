// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Cookies.Infrastructure;
using Microsoft.AspNet.Authentication.DataHandler;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Authentication.Cookies
{
    public class CookieAuthenticationMiddleware : AuthenticationMiddleware<CookieAuthenticationOptions>
    {
        public CookieAuthenticationMiddleware(
            [NotNull] RequestDelegate next,
            [NotNull] IDataProtectionProvider dataProtectionProvider,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IUrlEncoder urlEncoder,
            [NotNull] IOptions<CookieAuthenticationOptions> options,
            ConfigureOptions<CookieAuthenticationOptions> configureOptions)
            : base(next, options, loggerFactory, urlEncoder, configureOptions)
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
                var dataProtector = dataProtectionProvider.CreateProtector(
                    typeof(CookieAuthenticationMiddleware).FullName, Options.AuthenticationScheme, "v2");
                Options.TicketDataFormat = new TicketDataFormat(dataProtector);
            }
            if (Options.CookieManager == null)
            {
                Options.CookieManager = new ChunkingCookieManager(urlEncoder);
            }
        }

        protected override AuthenticationHandler<CookieAuthenticationOptions> CreateHandler()
        {
            return new CookieAuthenticationHandler();
        }
    }
}