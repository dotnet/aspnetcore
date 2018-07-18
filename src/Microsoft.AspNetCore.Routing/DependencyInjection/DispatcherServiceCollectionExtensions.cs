// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.EndpointFinders;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matchers;
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

            // Collect all data sources from DI.
            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<DispatcherOptions>, ConfigureDispatcherOptions>());

            // Allow global access to the list of endpoints.
            services.TryAddSingleton<CompositeEndpointDataSource>(s =>
            {
                var options = s.GetRequiredService<IOptions<DispatcherOptions>>();
                return new CompositeEndpointDataSource(options.Value.DataSources);
            });

            //
            // Default matcher implementation
            //
            services.TryAddSingleton<MatchProcessorFactory, DefaultMatchProcessorFactory>();
            services.TryAddSingleton<MatcherFactory, DfaMatcherFactory>();
            services.TryAddTransient<DfaMatcherBuilder>();

            // Link generation related services
            services.TryAddSingleton<IEndpointFinder<string>, NameBasedEndpointFinder>();
            services.TryAddSingleton<IEndpointFinder<RouteValuesBasedEndpointFinderContext>, RouteValuesBasedEndpointFinder>();
            services.TryAddSingleton<LinkGenerator, DefaultLinkGenerator>();
            //
            // Endpoint Selection
            //
            services.TryAddSingleton<EndpointSelector>();
            services.TryAddSingleton<EndpointConstraintCache>();

            // Will be cached by the EndpointSelector
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IEndpointConstraintProvider, DefaultEndpointConstraintProvider>());

            services.TryAddSingleton(typeof(DispatcherMarkerService));

            return services;
        }

        public static IServiceCollection AddDispatcher(this IServiceCollection services, Action<DispatcherOptions> configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddDispatcher();
            if (configuration != null)
            {
                services.Configure<DispatcherOptions>(configuration);
            }

            return services;
        }
    }
}
