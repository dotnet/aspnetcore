// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Facebook.Internal;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FacebookAuthenticationOptionsExtensions
    {
        public static IServiceCollection AddFacebookAuthentication(this IServiceCollection services) 
            => services.AddFacebookAuthentication(FacebookDefaults.AuthenticationScheme, _ => { });

        public static IServiceCollection AddFacebookAuthentication(this IServiceCollection services, Action<FacebookOptions> configureOptions) 
            => services.AddFacebookAuthentication(FacebookDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddFacebookAuthentication(this IServiceCollection services, string authenticationScheme, Action<FacebookOptions> configureOptions)
        {
            services.AddSingleton<ConfigureDefaultOptions<FacebookOptions>, FacebookConfigureOptions>();
            return services.AddOAuthAuthentication<FacebookOptions, FacebookHandler>(authenticationScheme, configureOptions);
        }
    }
}
