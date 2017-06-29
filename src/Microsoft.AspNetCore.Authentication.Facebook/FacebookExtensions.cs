// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FacebookAuthenticationOptionsExtensions
    {
        public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder)
            => builder.AddFacebook(FacebookDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, Action<FacebookOptions> configureOptions)
            => builder.AddFacebook(FacebookDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, string authenticationScheme, Action<FacebookOptions> configureOptions)
            => builder.AddOAuth<FacebookOptions, FacebookHandler>(authenticationScheme, configureOptions);


        // REMOVE below once callers have been updated
        public static IServiceCollection AddFacebookAuthentication(this IServiceCollection services) 
            => services.AddFacebookAuthentication(FacebookDefaults.AuthenticationScheme, _ => { });

        public static IServiceCollection AddFacebookAuthentication(this IServiceCollection services, Action<FacebookOptions> configureOptions) 
            => services.AddFacebookAuthentication(FacebookDefaults.AuthenticationScheme, configureOptions);

        public static IServiceCollection AddFacebookAuthentication(this IServiceCollection services, string authenticationScheme, Action<FacebookOptions> configureOptions)
            => services.AddOAuthAuthentication<FacebookOptions, FacebookHandler>(authenticationScheme, configureOptions);
    }
}
