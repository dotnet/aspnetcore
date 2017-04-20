// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenIdConnectExtensions
    {
        /// <summary>
        /// Adds OpenIdConnect authentication with options bound against the "OpenIdConnect" section 
        /// from the IConfiguration in the service container.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddOpenIdConnectAuthentication(this IServiceCollection services)
        {
            services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, OpenIdConnectConfigureOptions>();
            return services.AddOpenIdConnectAuthentication(OpenIdConnectDefaults.AuthenticationScheme, _ => { });
        }

        public static IServiceCollection AddOpenIdConnectAuthentication(this IServiceCollection services, Action<OpenIdConnectOptions> configureOptions) 
            => services.AddOpenIdConnectAuthentication(OpenIdConnectDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddOpenIdConnectAuthentication(this IServiceCollection services, string authenticationScheme, Action<OpenIdConnectOptions> configureOptions)
        {
            return services.AddScheme<OpenIdConnectOptions, OpenIdConnectHandler>(authenticationScheme, authenticationScheme, configureOptions);
        }
    }
}
