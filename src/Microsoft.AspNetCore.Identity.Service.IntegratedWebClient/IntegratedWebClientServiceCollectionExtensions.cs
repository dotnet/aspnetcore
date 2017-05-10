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
        public static IServiceCollection AddIntegratedWebClient(
            this IServiceCollection services,
            Action<IntegratedWebClientOptions> action)
        {
            services.Configure(action);
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IConfigureOptions<OpenIdConnectOptions>, IntegratedWebClientOpenIdConnectOptionsSetup>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<MvcOptions>, IntegratedWebclientMvcOptionsSetup>());

            return services;
        }

        public static IServiceCollection AddIntegratedWebClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddIntegratedWebClient(options => configuration.Bind(options));
            return services;
        }
    }
}
