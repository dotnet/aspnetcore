// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.DataProtection;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Cookies.Interop;

namespace Owin
{
    public static class CookieAuthenticationExtensions
    {
        public static IAppBuilder UseCookieAuthentication(
            this IAppBuilder app,
            CookieAuthenticationOptions options,
            DataProtectionProvider dataProtectionProvider,
            PipelineStage stage = PipelineStage.Authenticate)
        {
            var dataProtector = dataProtectionProvider.CreateProtector(
                "Microsoft.AspNet.Authentication.Cookies.CookieAuthenticationMiddleware", // full name of the ASP.NET 5 type
                options.AuthenticationType, "v2");
            options.TicketDataFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector));

            return app.UseCookieAuthentication(options, stage);
        }
    }
}