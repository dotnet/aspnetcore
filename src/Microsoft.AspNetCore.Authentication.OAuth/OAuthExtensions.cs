// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OAuthExtensions
    {
        public static IServiceCollection AddOAuthAuthentication(this IServiceCollection services, string authenticationScheme, Action<OAuthOptions> configureOptions)
        {
            return services.AddOAuthAuthentication<OAuthOptions, OAuthHandler<OAuthOptions>>(authenticationScheme, configureOptions);
        }

        public static IServiceCollection AddOAuthAuthentication<TOptions, THandler>(this IServiceCollection services, string authenticationScheme, Action<TOptions> configureOptions)
            where TOptions : OAuthOptions, new()
            where THandler : OAuthHandler<TOptions>
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, OAuthPostConfigureOptions<TOptions, THandler>>());
            return services.AddRemoteScheme<TOptions, THandler>(authenticationScheme, authenticationScheme, configureOptions);
        }
    }
}
