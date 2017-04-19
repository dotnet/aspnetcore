// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CookieExtensions
    {
        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services) => services.AddCookieAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services, string authenticationScheme) => services.AddCookieAuthentication(authenticationScheme, configureOptions: null);

        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services, Action<CookieAuthenticationOptions> configureOptions) =>
            services.AddCookieAuthentication(CookieAuthenticationDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services, string authenticationScheme, Action<CookieAuthenticationOptions> configureOptions) =>
            services.AddScheme<CookieAuthenticationOptions, CookieAuthenticationHandler>(authenticationScheme, configureOptions);
    }
}
