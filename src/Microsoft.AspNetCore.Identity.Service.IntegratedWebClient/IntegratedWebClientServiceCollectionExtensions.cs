// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service.IntegratedWebClient
{
    public static class IntegratedWebClientServiceCollectionExtensions
    {
        public static IServiceCollection WithIntegratedWebClient(
            this IServiceCollection services,
            Action<IntegratedWebClientOptions> action)
        {
            services.Configure(action);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<OpenIdConnectOptions>, IntegratedWebClientOpenIdConnectOptionsSetup>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<MvcOptions>, IntegratedWebclientMvcOptionsSetup>());

            return services;
        }

        public static IServiceCollection WithIntegratedWebClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.WithIntegratedWebClient(options => configuration.Bind(options));
            return services;
        }

        public static IServiceCollection WithIntegratedWebClient(this IServiceCollection services)
        {
            services.TryAddTransient<IConfigureOptions<IntegratedWebClientOptions>, DefaultSetup>();
            services.WithIntegratedWebClient(_ => { });
            return services;
        }

        private class DefaultSetup : ConfigureOptions<IntegratedWebClientOptions>
        {
            public DefaultSetup(IConfiguration configuration)
                : base(options => configuration.GetSection(OpenIdConnectDefaults.AuthenticationScheme).Bind(options))
            {
            }
        }
    }
}
