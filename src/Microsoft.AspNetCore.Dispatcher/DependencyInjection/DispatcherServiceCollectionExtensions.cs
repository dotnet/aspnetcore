// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DispatcherServiceCollectionExtensions
    {
        public static IServiceCollection AddDispatcher(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Adds the EndpointMiddleware at the end of the pipeline if the DispatcherMiddleware is in use.
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, DispatcherEndpointStartupFilter>());

            // Adds a default dispatcher which will collect all data sources and endpoint selectors from DI.
            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<DispatcherOptions>, DefaultDispatcherConfigureOptions>());

            //
            // Addresses + Templates
            //
            services.TryAddSingleton<AddressTable, DefaultAddressTable>();
            services.TryAddSingleton<TemplateFactory, DefaultTemplateFactory>();
            services.TryAddSingleton<ITemplateFactoryComponent, RoutePatternTemplateFactory>();
            services.TryAddSingleton<TemplateAddressSelector>();

            //
            // Misc Infrastructure
            //
            services.TryAddSingleton<RoutePatternBinderFactory>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHandlerFactory, TemplateEndpointHandlerFactory>());

            return services;
        }
    }
}
