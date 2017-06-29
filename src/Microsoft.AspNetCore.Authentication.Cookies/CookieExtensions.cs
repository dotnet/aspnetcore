// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CookieExtensions
    {
        public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder)
            => builder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, string authenticationScheme)
            => builder.AddCookie(authenticationScheme, configureOptions: null);

        public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, Action<CookieAuthenticationOptions> configureOptions) 
            => builder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, string authenticationScheme, Action<CookieAuthenticationOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureCookieAuthenticationOptions>());
            return builder.AddScheme<CookieAuthenticationOptions, CookieAuthenticationHandler>(authenticationScheme, configureOptions);
        }


        // REMOVE below once callers have been updated
        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services) => services.AddCookieAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services, string authenticationScheme) => services.AddCookieAuthentication(authenticationScheme, configureOptions: null);

        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services, Action<CookieAuthenticationOptions> configureOptions) =>
            services.AddCookieAuthentication(CookieAuthenticationDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services, string authenticationScheme, Action<CookieAuthenticationOptions> configureOptions)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureCookieAuthenticationOptions>());
            return services.AddScheme<CookieAuthenticationOptions, CookieAuthenticationHandler>(authenticationScheme, configureOptions);
        }
    }
}
