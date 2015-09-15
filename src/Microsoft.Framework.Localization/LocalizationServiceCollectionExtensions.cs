// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Localization;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding localization servics to the DI container.
    /// </summary>
    public static class LocalizationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services required for application localization.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddLocalization([NotNull] this IServiceCollection services)
        {
            return AddLocalization(services, setupAction: null);
        }

        /// <summary>
        /// Adds services required for application localization.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="setupAction">An action to configure the <see cref="LocalizationOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddLocalization(
            [NotNull] this IServiceCollection services,
            Action<LocalizationOptions> setupAction)
        {
            services.TryAdd(new ServiceDescriptor(
                typeof(IStringLocalizerFactory),
                typeof(ResourceManagerStringLocalizerFactory),
                ServiceLifetime.Singleton));
            services.TryAdd(new ServiceDescriptor(
                typeof(IStringLocalizer<>),
                typeof(StringLocalizer<>),
                ServiceLifetime.Transient));

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }
            services.AddOptions();
            return services;
        }
    }
}