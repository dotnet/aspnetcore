// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Antiforgery;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.OptionsModel;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAntiforgery(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddDataProtection();

            // Don't overwrite any options setups that a user may have added.
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<AntiforgeryOptions>, AntiforgeryOptionsSetup>());

            services.TryAddSingleton<IAntiforgery, DefaultAntiforgery>();
            services.TryAddSingleton<IAntiforgeryTokenGenerator, DefaultAntiforgeryTokenGenerator>();
            services.TryAddSingleton<IAntiforgeryTokenSerializer, DefaultAntiforgeryTokenSerializer>();
            services.TryAddSingleton<IAntiforgeryTokenStore, DefaultAntiforgeryTokenStore>();
            services.TryAddSingleton<IClaimUidExtractor, DefaultClaimUidExtractor>();
            services.TryAddScoped<IAntiforgeryContextAccessor, DefaultAntiforgeryContextAccessor>();
            services.TryAddSingleton<IAntiforgeryAdditionalDataProvider, DefaultAntiforgeryAdditionalDataProvider>();
            return services;
        }

        public static IServiceCollection ConfigureAntiforgery(
            this IServiceCollection services,
            Action<AntiforgeryOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.Configure(setupAction);
            return services;
        }
    }
}
