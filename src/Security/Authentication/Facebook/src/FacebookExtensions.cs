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

        public static AuthenticationBuilder AddFacebook<TService>(this AuthenticationBuilder builder, Action<FacebookOptions, TService> configureOptions) where TService : class
            => builder.AddFacebook(FacebookDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, string authenticationScheme, Action<FacebookOptions> configureOptions)
            => builder.AddFacebook(authenticationScheme, FacebookDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddFacebook<TService>(this AuthenticationBuilder builder, string authenticationScheme, Action<FacebookOptions, TService> configureOptions) where TService : class
            => builder.AddFacebook(authenticationScheme, FacebookDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<FacebookOptions> configureOptions)
        {
            Action<FacebookOptions, IServiceProvider> configureOptionsWithServices;
            if (configureOptions == null)
            {
                configureOptionsWithServices = null;
            }
            else
            {
                configureOptionsWithServices = (options, _) => configureOptions(options);
            }

            return builder.AddFacebook(authenticationScheme, displayName, configureOptionsWithServices);
        }

        public static AuthenticationBuilder AddFacebook<TService>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<FacebookOptions, TService> configureOptions) where TService : class
            => builder.AddOAuth<FacebookOptions, FacebookHandler, TService>(authenticationScheme, displayName, configureOptions);
    }
}
