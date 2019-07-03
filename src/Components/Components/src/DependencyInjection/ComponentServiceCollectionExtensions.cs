// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for components.
    /// </summary>
    public static class ComponentServiceCollectionExtensions
    {
        /// <summary>
        /// Adds components services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceProvider"/>.</returns>
        public static IServiceCollection AddComponents(this IServiceCollection services)
        {
            services.TryAddSingleton<ComponentFactory>();
            return services;
        }
    }
}
