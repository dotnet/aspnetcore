// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MicrosoftAccountExtensions
    {
        /// <summary>
        /// Adds MicrosoftAccount authentication with options bound against the "Microsoft" section 
        /// from the IConfiguration in the service container.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMicrosoftAccountAuthentication(this IServiceCollection services)
        {
            services.AddSingleton<IConfigureOptions<MicrosoftAccountOptions>, MicrosoftAccountConfigureOptions>();
            return services.AddMicrosoftAccountAuthentication(MicrosoftAccountDefaults.AuthenticationScheme, o => { });
        }

        public static IServiceCollection AddMicrosoftAccountAuthentication(this IServiceCollection services, Action<MicrosoftAccountOptions> configureOptions) =>
            services.AddMicrosoftAccountAuthentication(MicrosoftAccountDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddMicrosoftAccountAuthentication(this IServiceCollection services, string authenticationScheme, Action<MicrosoftAccountOptions> configureOptions)
        {
            return services.AddScheme<MicrosoftAccountOptions, MicrosoftAccountHandler>(authenticationScheme, authenticationScheme, configureOptions);
        }
    }
}