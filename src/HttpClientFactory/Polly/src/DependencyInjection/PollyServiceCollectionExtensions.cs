// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Polly.Registry;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
   /// Provides convenience extension methods to register <see cref="IPolicyRegistry{String}"/> and 
   /// <see cref="IReadOnlyPolicyRegistry{String}"/> in the service collection.
    /// </summary>
    public static class PollyServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an empty <see cref="PolicyRegistry"/> in the service collection with service types
        /// <see cref="IPolicyRegistry{String}"/>, and <see cref="IReadOnlyPolicyRegistry{String}"/> and returns
        /// the newly created registry.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The newly created <see cref="PolicyRegistry"/>.</returns>
        public static IPolicyRegistry<string> AddPolicyRegistry(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Create an empty registry, register and return it as an instance. This is the best way to get a 
            // single instance registered using both interfaces.
            var registry = new PolicyRegistry();
            services.AddSingleton<IPolicyRegistry<string>>(registry);
            services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);

            return registry;
        }

        /// <summary>
        /// Registers the provided <see cref="IPolicyRegistry{String}"/> in the service collection with service types
        /// <see cref="IPolicyRegistry{String}"/>, and <see cref="IReadOnlyPolicyRegistry{String}"/> and returns
        /// the provided registry.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="registry">The <see cref="IPolicyRegistry{String}"/>.</param>
        /// <returns>The provided <see cref="IPolicyRegistry{String}"/>.</returns>
        public static IPolicyRegistry<string> AddPolicyRegistry(this IServiceCollection services, IPolicyRegistry<string> registry)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            services.AddSingleton<IPolicyRegistry<string>>(registry);
            services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);

            return registry;
        }
    }
}
