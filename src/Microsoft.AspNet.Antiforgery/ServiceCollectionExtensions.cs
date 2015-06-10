// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Antiforgery;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAntiforgery([NotNull] this IServiceCollection services)
        {
            services.AddDataProtection();
            services.AddWebEncoders();

            services.TryAdd(ServiceDescriptor.Singleton<IClaimUidExtractor, DefaultClaimUidExtractor>());
            services.TryAdd(ServiceDescriptor.Singleton<Antiforgery, Antiforgery>());
            services.TryAdd(ServiceDescriptor.Scoped<IAntiforgeryContextAccessor, AntiforgeryContextAccessor>());
            services.TryAdd(
                ServiceDescriptor.Singleton<IAntiforgeryAdditionalDataProvider, DefaultAntiforgeryAdditionalDataProvider>());
            return services;
        }

        public static IServiceCollection ConfigureAntiforgery(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<AntiforgeryOptions> setupAction)
        {
            services.Configure(setupAction);
            return services;
        }
    }
}
