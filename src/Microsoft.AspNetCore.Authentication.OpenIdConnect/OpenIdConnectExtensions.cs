// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenIdConnectExtensions
    {
        public static IServiceCollection AddOpenIdConnectAuthentication(this IServiceCollection services)
            => services.AddOpenIdConnectAuthentication(OpenIdConnectDefaults.AuthenticationScheme, _ => { });

        public static IServiceCollection AddOpenIdConnectAuthentication(this IServiceCollection services, Action<OpenIdConnectOptions> configureOptions) 
            => services.AddOpenIdConnectAuthentication(OpenIdConnectDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddOpenIdConnectAuthentication(this IServiceCollection services, string authenticationScheme, Action<OpenIdConnectOptions> configureOptions)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OpenIdConnectOptions>, OpenIdConnectPostConfigureOptions>());
            services.AddSingleton<ConfigureDefaultOptions<OpenIdConnectOptions>, OpenIdConnectConfigureOptions>();
            return services.AddRemoteScheme<OpenIdConnectOptions, OpenIdConnectHandler>(authenticationScheme, authenticationScheme, configureOptions);
        }
    }
}
