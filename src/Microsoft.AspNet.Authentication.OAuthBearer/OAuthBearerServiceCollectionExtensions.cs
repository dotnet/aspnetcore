// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.OAuthBearer;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods to add OAuth Bearer authentication capabilities to an HTTP application pipeline
    /// </summary>
    public static class OAuthBearerServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureOAuthBearerAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<OAuthBearerAuthenticationOptions> configure)
        {
            return services.ConfigureOAuthBearerAuthentication(configure, optionsName: "");
        }

        public static IServiceCollection ConfigureOAuthBearerAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<OAuthBearerAuthenticationOptions> configure, string optionsName)
        {
            return services.Configure(configure, optionsName);
        }

        public static IServiceCollection ConfigureOAuthBearerAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config)
        {
            return services.ConfigureOAuthBearerAuthentication(config, optionsName: "");
        }

        public static IServiceCollection ConfigureOAuthBearerAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config, string optionsName)
        {
            return services.Configure<OAuthBearerAuthenticationOptions>(config, optionsName);
        }
    }
}
