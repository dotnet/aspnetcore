// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods provided by the cookies authentication middleware
    /// </summary>
    public static class CookieServiceCollectionExtensions
    {
        public static IServiceCollection AddCookieAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<CookieAuthenticationOptions> configure)
        {
            return services.Configure(configure);
        }

        public static IServiceCollection AddCookieAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config)
        {
            return services.Configure<CookieAuthenticationOptions>(config);
        }
    }
}