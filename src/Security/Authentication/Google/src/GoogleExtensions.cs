// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GoogleExtensions
    {
        public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder)
            => builder.AddGoogle(GoogleDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, Action<GoogleOptions> configureOptions)
            => builder.AddGoogle(GoogleDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddGoogle<TService>(this AuthenticationBuilder builder, Action<GoogleOptions, TService> configureOptions) where TService : class
            => builder.AddGoogle(GoogleDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, string authenticationScheme, Action<GoogleOptions> configureOptions)
            => builder.AddGoogle(authenticationScheme, GoogleDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddGoogle<TService>(this AuthenticationBuilder builder, string authenticationScheme, Action<GoogleOptions, TService> configureOptions) where TService : class
            => builder.AddGoogle(authenticationScheme, GoogleDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<GoogleOptions> configureOptions)
        {
            Action<GoogleOptions, IServiceProvider> configureOptionsWithServices;
            if (configureOptions == null)
            {
                configureOptionsWithServices = null;
            }
            else
            {
                configureOptionsWithServices = (options, _) => configureOptions(options);
            }

            return builder.AddGoogle(authenticationScheme, displayName, configureOptionsWithServices);
        }

        public static AuthenticationBuilder AddGoogle<TService>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<GoogleOptions, TService> configureOptions) where TService : class
            => builder.AddOAuth<GoogleOptions, GoogleHandler, TService>(authenticationScheme, displayName, configureOptions);
    }
}
