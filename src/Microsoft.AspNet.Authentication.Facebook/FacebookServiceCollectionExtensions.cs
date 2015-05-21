// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Facebook;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for using <see cref="FacebookAuthenticationMiddleware"/>.
    /// </summary>
    public static class FacebookServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureFacebookAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<FacebookAuthenticationOptions> configure)
        {
            return services.ConfigureFacebookAuthentication(configure, optionsName: "");
        }

        public static IServiceCollection ConfigureFacebookAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<FacebookAuthenticationOptions> configure, string optionsName)
        {
            return services.Configure(configure, optionsName);
        }

        public static IServiceCollection ConfigureFacebookAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config)
        {
            return services.ConfigureFacebookAuthentication(config, optionsName: "");
        }

        public static IServiceCollection ConfigureFacebookAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config, string optionsName)
        {
            return services.Configure<FacebookAuthenticationOptions>(config, optionsName);
        }
    }
}