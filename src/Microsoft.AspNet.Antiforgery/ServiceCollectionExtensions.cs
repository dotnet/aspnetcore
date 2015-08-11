// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Antiforgery;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAntiforgery([NotNull] this IServiceCollection services)
        {
            services.AddDataProtection();
            services.AddWebEncoders();

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
            [NotNull] this IServiceCollection services,
            [NotNull] Action<AntiforgeryOptions> setupAction)
        {
            services.Configure(setupAction);
            return services;
        }
    }
}
