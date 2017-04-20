// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TwitterExtensions
    {
        /// <summary>
        /// Adds Twitter authentication with options bound against the "Twitter" section 
        /// from the IConfiguration in the service container.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTwitterAuthentication(this IServiceCollection services)
        {
            services.AddSingleton<IConfigureOptions<TwitterOptions>, TwitterConfigureOptions>();
            return services.AddTwitterAuthentication(TwitterDefaults.AuthenticationScheme, _ => { });
        }

        public static IServiceCollection AddTwitterAuthentication(this IServiceCollection services, Action<TwitterOptions> configureOptions)
            => services.AddTwitterAuthentication(TwitterDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddTwitterAuthentication(this IServiceCollection services, string authenticationScheme, Action<TwitterOptions> configureOptions)
        {
            return services.AddScheme<TwitterOptions, TwitterHandler>(authenticationScheme, authenticationScheme, configureOptions);
        }
    }
}
