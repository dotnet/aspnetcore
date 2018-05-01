// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionHostedServiceExtensions
    {
        /// <summary>
        /// Add an <see cref="IHostedService"/> registration for the given type.
        /// </summary>
        /// <typeparam name="THostedService">An <see cref="IHostedService"/> to register.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
        /// <returns>The original <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddHostedService<THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
            => services.AddTransient<IHostedService, THostedService>();
    }
}
