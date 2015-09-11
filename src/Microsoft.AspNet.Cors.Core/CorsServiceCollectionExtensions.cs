// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cors;
using Microsoft.AspNet.Cors.Core;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// The <see cref="IServiceCollection"/> extensions for enabling CORS support.
    /// </summary>
    public static class CorsServiceCollectionExtensions
    {
        /// <summary>
        /// Add services needed to support CORS to the given <paramref name="serviceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The service collection to which CORS services are added.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddCors(this IServiceCollection serviceCollection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddOptions();
            serviceCollection.TryAdd(ServiceDescriptor.Transient<ICorsService, CorsService>());
            serviceCollection.TryAdd(ServiceDescriptor.Transient<ICorsPolicyProvider, DefaultCorsPolicyProvider>());
            return serviceCollection;
        }

        /// <summary>
        /// Add services needed to support CORS to the given <paramref name="serviceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The service collection to which CORS services are added.</param>
        /// <param name="configure">A delegate which is run to configure the services.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddCors(
            this IServiceCollection serviceCollection,
            Action<CorsOptions> configure)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            serviceCollection.Configure(configure);
            return serviceCollection.AddCors();
        }
    }
}