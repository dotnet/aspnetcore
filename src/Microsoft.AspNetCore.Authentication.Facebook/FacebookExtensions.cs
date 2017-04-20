// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FacebookAuthenticationOptionsExtensions
    {
        /// <summary>
        /// Adds facebook authentication with options bound against the "Facebook" section 
        /// from the IConfiguration in the service container.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddFacebookAuthentication(this IServiceCollection services)
        {
            services.AddSingleton<IConfigureOptions<FacebookOptions>, FacebookConfigureOptions>();
            return services.AddFacebookAuthentication(FacebookDefaults.AuthenticationScheme, _ => { });
        }

        public static IServiceCollection AddFacebookAuthentication(this IServiceCollection services, Action<FacebookOptions> configureOptions) 
            => services.AddFacebookAuthentication(FacebookDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddFacebookAuthentication(this IServiceCollection services, string authenticationScheme, Action<FacebookOptions> configureOptions)
        {
            return services.AddScheme<FacebookOptions, FacebookHandler>(authenticationScheme, authenticationScheme, configureOptions);
        }
    }
}
