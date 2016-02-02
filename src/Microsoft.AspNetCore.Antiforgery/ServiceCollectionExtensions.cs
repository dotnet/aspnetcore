// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Antiforgery.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

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

            services.TryAddSingleton<ObjectPoolProvider>(new DefaultObjectPoolProvider());
            services.TryAddSingleton<ObjectPool<AntiforgerySerializationContext>>(serviceProvider =>
            {
                var provider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
                var policy = new AntiforgerySerializationContextPooledObjectPolicy();
                return provider.Create(policy);
            });

            return services;
        }

        public static IServiceCollection AddAntiforgery(
            this IServiceCollection services,
            Action<AntiforgeryOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddAntiforgery();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return services;
        }
    }
}
