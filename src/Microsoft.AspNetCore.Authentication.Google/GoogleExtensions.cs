// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GoogleExtensions
    {
        /// <summary>
        /// Adds google authentication with options bound against the "Google" section 
        /// from the IConfiguration in the service container.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddGoogleAuthentication(this IServiceCollection services)
        {
            services.AddSingleton<IConfigureOptions<GoogleOptions>, GoogleConfigureOptions>();
            return services.AddGoogleAuthentication(GoogleDefaults.AuthenticationScheme, _ => { });
        }

        public static IServiceCollection AddGoogleAuthentication(this IServiceCollection services, Action<GoogleOptions> configureOptions) 
            => services.AddGoogleAuthentication(GoogleDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddGoogleAuthentication(this IServiceCollection services, string authenticationScheme, Action<GoogleOptions> configureOptions)
        {
            return services.AddScheme<GoogleOptions, GoogleHandler>(authenticationScheme, authenticationScheme, configureOptions);
        }
    }
}
