// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure OpenIdConnect authentication options
    /// </summary>
    public static class OpenIdConnectServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureOpenIdConnectAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<OpenIdConnectAuthenticationOptions> configure)
        {
            return services.ConfigureOpenIdConnectAuthentication(configure, optionsName: "");
        }

        public static IServiceCollection ConfigureOpenIdConnectAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<OpenIdConnectAuthenticationOptions> configure, string optionsName)
        {
            return services.Configure(configure, optionsName);
        }

        public static IServiceCollection ConfigureOpenIdConnectAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config)
        {
            return services.ConfigureOpenIdConnectAuthentication(config, optionsName: "");
        }

        public static IServiceCollection ConfigureOpenIdConnectAuthentication([NotNull] this IServiceCollection services, [NotNull] IConfiguration config, string optionsName)
        {
            return services.Configure<OpenIdConnectAuthenticationOptions>(config, optionsName);
        }
    }
}
