// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Google;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for using <see cref="GoogleAuthenticationMiddleware"/>.
    /// </summary>
    public static class GoogleServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureGoogleAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<GoogleAuthenticationOptions> configure)
        {
            return services.ConfigureGoogleAuthentication(configure, optionsName: "");
        }

        public static IServiceCollection ConfigureGoogleAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<GoogleAuthenticationOptions> configure, string optionsName)
        {
            return services.Configure(configure, optionsName);
        }

        public static IServiceCollection ConfigureGoogleAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config)
        {
            return services.ConfigureGoogleAuthentication(config, optionsName: "");
        }

        public static IServiceCollection ConfigureGoogleAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config, string optionsName)
        {
            return services.Configure<GoogleAuthenticationOptions>(config, optionsName);
        }
    }
}