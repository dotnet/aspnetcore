// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Allows registering and configuring Data Protection in the application.
    /// </summary>
    public static class DataProtectionServiceCollectionExtensions
    {
        /// <summary>
        /// Adds default Data Protection services to an <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to which to add DataProtection services.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddDataProtection(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.TryAdd(DataProtectionServices.GetDefaultServices());
            return services;
        }

        /// <summary>
        /// Adds default Data Protection services to an <see cref="IServiceCollection"/> and configures the behavior of the Data Protection system.
        /// </summary>
        /// <param name="services">A service collection to which Data Protection has already been added.</param>
        /// <param name="configure">A callback which takes a <see cref="DataProtectionConfiguration"/> parameter.
        /// This callback will be responsible for configuring the system.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddDataProtection(this IServiceCollection services, Action<DataProtectionConfiguration> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configure(new DataProtectionConfiguration(services));
            return services.AddDataProtection();
        }
    }
}
