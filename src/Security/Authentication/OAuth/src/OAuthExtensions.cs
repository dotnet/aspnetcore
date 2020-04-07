// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OAuthExtensions
    {
        public static AuthenticationBuilder AddOAuth(this AuthenticationBuilder builder, string authenticationScheme, Action<OAuthOptions> configureOptions)
            => builder.AddOAuth<OAuthOptions, OAuthHandler<OAuthOptions>>(authenticationScheme, configureOptions);

        public static AuthenticationBuilder AddOAuth<TService>(this AuthenticationBuilder builder, string authenticationScheme, Action<OAuthOptions, TService> configureOptions) where TService : class
            => builder.AddOAuth<OAuthOptions, OAuthHandler<OAuthOptions>, TService>(authenticationScheme, configureOptions);

        public static AuthenticationBuilder AddOAuth(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<OAuthOptions> configureOptions)
            => builder.AddOAuth<OAuthOptions, OAuthHandler<OAuthOptions>>(authenticationScheme, displayName, configureOptions);

        public static AuthenticationBuilder AddOAuth<TService>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<OAuthOptions, TService> configureOptions) where TService : class
            => builder.AddOAuth<OAuthOptions, OAuthHandler<OAuthOptions>, TService>(authenticationScheme, displayName, configureOptions);

        public static AuthenticationBuilder AddOAuth<TOptions, THandler>(this AuthenticationBuilder builder, string authenticationScheme, Action<TOptions> configureOptions)
            where TOptions : OAuthOptions, new()
            where THandler : OAuthHandler<TOptions>
            => builder.AddOAuth<TOptions, THandler>(authenticationScheme, OAuthDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddOAuth<TOptions, THandler, TService>(this AuthenticationBuilder builder, string authenticationScheme, Action<TOptions, TService> configureOptions)
            where TOptions : OAuthOptions, new()
            where THandler : OAuthHandler<TOptions>
            where TService : class
            => builder.AddOAuth<TOptions, THandler, TService>(authenticationScheme, OAuthDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddOAuth<TOptions, THandler>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<TOptions> configureOptions)
            where TOptions : OAuthOptions, new()
            where THandler : OAuthHandler<TOptions>
        {
            Action<TOptions, IServiceProvider> configureOptionsWithServices;
            if (configureOptions == null)
            {
                configureOptionsWithServices = null;
            }
            else
            {
                configureOptionsWithServices = (options, _) => configureOptions(options);
            }

            return builder.AddOAuth<TOptions, THandler, IServiceProvider>(authenticationScheme, displayName, configureOptionsWithServices);
        }

        public static AuthenticationBuilder AddOAuth<TOptions, THandler, TService>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<TOptions, TService> configureOptions)
            where TOptions : OAuthOptions, new()
            where THandler : OAuthHandler<TOptions>
            where TService : class
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, OAuthPostConfigureOptions<TOptions, THandler>>());
            return builder.AddRemoteScheme<TOptions, THandler, TService>(authenticationScheme, displayName, configureOptions);
        }
    }
}
