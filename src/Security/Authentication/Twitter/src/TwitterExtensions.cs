// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TwitterExtensions
    {
        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder)
            => builder.AddTwitter(TwitterDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, Action<TwitterOptions> configureOptions)
            => builder.AddTwitter(TwitterDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddTwitter<TService>(this AuthenticationBuilder builder, Action<TwitterOptions, TService> configureOptions) where TService : class
            => builder.AddTwitter(TwitterDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, string authenticationScheme, Action<TwitterOptions> configureOptions)
            => builder.AddTwitter(authenticationScheme, TwitterDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddTwitter<TService>(this AuthenticationBuilder builder, string authenticationScheme, Action<TwitterOptions, TService> configureOptions) where TService : class
            => builder.AddTwitter(authenticationScheme, TwitterDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<TwitterOptions> configureOptions)
        {
            Action<TwitterOptions, IServiceProvider> configureOptionsWithServices;
            if (configureOptions == null)
            {
                configureOptionsWithServices = null;
            }
            else
            {
                configureOptionsWithServices = (options, _) => configureOptions(options);
            }

            return builder.AddTwitter(authenticationScheme, displayName, configureOptionsWithServices);
        }

        public static AuthenticationBuilder AddTwitter<TService>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<TwitterOptions, TService> configureOptions) where TService : class
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TwitterOptions>, TwitterPostConfigureOptions>());
            return builder.AddRemoteScheme<TwitterOptions, TwitterHandler, TService>(authenticationScheme, displayName, configureOptions);
        }
    }
}
