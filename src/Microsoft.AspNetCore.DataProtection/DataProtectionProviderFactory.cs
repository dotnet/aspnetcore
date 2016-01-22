// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Contains static factory methods for creating <see cref="IDataProtectionProvider"/> instances.
    /// </summary>
    internal static class DataProtectionProviderFactory
    {
        /// <summary>
        /// Creates an <see cref="IDataProtectionProvider"/> given an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="options">The global options to use when creating the provider.</param>
        /// <param name="services">Provides mandatory services for use by the provider.</param>
        /// <returns>An <see cref="IDataProtectionProvider"/>.</returns>
        public static IDataProtectionProvider GetProviderFromServices(DataProtectionOptions options, IServiceProvider services)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return GetProviderFromServices(options, services, mustCreateImmediately: false);
        }

        internal static IDataProtectionProvider GetProviderFromServices(DataProtectionOptions options, IServiceProvider services, bool mustCreateImmediately)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            IDataProtectionProvider dataProtectionProvider = null;

            // If we're being asked to create the provider immediately, then it means that
            // we're already in a call to GetService, and we're responsible for supplying
            // the default implementation ourselves. We can't call GetService again or
            // else we risk stack diving.
            if (!mustCreateImmediately)
            {
                dataProtectionProvider = services.GetService<IDataProtectionProvider>();
            }

            // If all else fails, create a keyring manually based on the other registered services.
            if (dataProtectionProvider == null)
            {
                var keyRingProvider = new KeyRingProvider(
                    keyManager: services.GetRequiredService<IKeyManager>(),
                    keyManagementOptions: services.GetService<IOptions<KeyManagementOptions>>()?.Value, // might be null
                    services: services);
                dataProtectionProvider = new KeyRingBasedDataProtectionProvider(keyRingProvider, services);
            }

            // Finally, link the provider to the supplied discriminator
            if (!String.IsNullOrEmpty(options.ApplicationDiscriminator))
            {
                dataProtectionProvider = dataProtectionProvider.CreateProtector(options.ApplicationDiscriminator);
            }

            return dataProtectionProvider;
        }
    }
}
