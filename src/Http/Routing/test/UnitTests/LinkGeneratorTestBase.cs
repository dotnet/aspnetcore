// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class LinkGeneratorTestBase
    {
        protected HttpContext CreateHttpContext(object ambientValues = null)
        {
            var httpContext = new DefaultHttpContext();

            var context = new EndpointSelectorContext
            {
                RouteValues = new RouteValueDictionary(ambientValues)
            };

            httpContext.Features.Set<IEndpointFeature>(context);
            httpContext.Features.Set<IRouteValuesFeature>(context);
            return httpContext;
        }

        protected ServiceCollection GetBasicServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddOptions();
            services.AddRouting();
            services.AddLogging();
            return services;
        }

        protected virtual void AddAdditionalServices(IServiceCollection services)
        {
        }

        private protected DefaultLinkGenerator CreateLinkGenerator(params Endpoint[] endpoints)
        {
            return CreateLinkGenerator(routeOptions: null, endpoints);
        }

        private protected DefaultLinkGenerator CreateLinkGenerator(RouteOptions routeOptions, params Endpoint[] endpoints)
        {
            return CreateLinkGenerator(routeOptions, configureServices: null, endpoints);
        }

        private protected DefaultLinkGenerator CreateLinkGenerator(
            RouteOptions routeOptions,
            Action<IServiceCollection> configureServices,
            params Endpoint[] endpoints)
        {
            return CreateLinkGenerator(routeOptions, configureServices, new[] { new DefaultEndpointDataSource(endpoints ?? Array.Empty<Endpoint>()) });
        }

        private protected DefaultLinkGenerator CreateLinkGenerator(EndpointDataSource[] dataSources)
        {
            return CreateLinkGenerator(routeOptions: null, configureServices: null, dataSources);
        }

        private protected DefaultLinkGenerator CreateLinkGenerator(
            RouteOptions routeOptions,
            Action<IServiceCollection> configureServices,
            EndpointDataSource[] dataSources)
        {
            var services = GetBasicServices();
            AddAdditionalServices(services);
            configureServices?.Invoke(services);

            routeOptions = routeOptions ?? new RouteOptions();
            dataSources = dataSources ?? Array.Empty<EndpointDataSource>();

            services.Configure<EndpointOptions>((o) =>
            {
                for (var i = 0; i < dataSources.Length; i++)
                {
                    o.DataSources.Add(dataSources[i]);
                }
            });

            var options = Options.Create(routeOptions);
            var serviceProvider = services.BuildServiceProvider();

            return new DefaultLinkGenerator(
                new DefaultParameterPolicyFactory(options, serviceProvider),
                serviceProvider.GetRequiredService<CompositeEndpointDataSource>(),
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                options,
                NullLogger<DefaultLinkGenerator>.Instance,
                serviceProvider);
        }
    }
}
