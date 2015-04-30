// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cors;
using Microsoft.AspNet.Cors.Core;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// The <see cref="IServiceCollection"/> extensions for enabling CORS support.
    /// </summary>
    public static class CorsServiceCollectionExtensions
    {
        /// <summary>
        /// Can be used to configure services in the <paramref name="serviceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The service collection which needs to be configured.</param>
        /// <param name="configure">A delegate which is run to configure the services.</param>
        /// <returns></returns>
        public static IServiceCollection ConfigureCors(
            [NotNull] this IServiceCollection serviceCollection,
            [NotNull] Action<CorsOptions> configure)
        {
            return serviceCollection.Configure(configure);
        }

        /// <summary>
        /// Add services needed to support CORS to the given <paramref name="serviceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The service collection to which CORS services are added.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddCors(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAdd(ServiceDescriptor.Transient<ICorsService, CorsService>());
            serviceCollection.TryAdd(ServiceDescriptor.Transient<ICorsPolicyProvider, DefaultCorsPolicyProvider>());
            return serviceCollection;
        }
    }
}