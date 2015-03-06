// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods provided by the cookies authentication middleware
    /// </summary>
    public static class CookieServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureCookieAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<CookieAuthenticationOptions> configure)
        {
            return services.ConfigureCookieAuthentication(configure, optionsName: "");
        }

        public static IServiceCollection ConfigureCookieAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<CookieAuthenticationOptions> configure, string optionsName)
        {
            return services.Configure(configure, optionsName);
        }

        public static IServiceCollection ConfigureCookieAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config)
        {
            return services.ConfigureCookieAuthentication(config, optionsName: "");
        }

        public static IServiceCollection ConfigureCookieAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config, string optionsName)
        {
            return services.Configure<CookieAuthenticationOptions>(config, optionsName);
        }
    }
}