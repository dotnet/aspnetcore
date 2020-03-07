// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class LinkGeneratorTestBase
    {
        protected HttpContext CreateHttpContext(object ambientValues = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues = new RouteValueDictionary(ambientValues);
            return httpContext;
        }

        protected ServiceCollection GetBasicServices()
        {
            var services = new ServiceCollection();
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
            return CreateLinkGenerator(configureServices: null, endpoints);
        }

        private protected DefaultLinkGenerator CreateLinkGenerator(
            Action<IServiceCollection> configureServices,
            params Endpoint[] endpoints)
        {
            return CreateLinkGenerator(configureServices, new[] { new DefaultEndpointDataSource(endpoints ?? Array.Empty<Endpoint>()) });
        }

        private protected DefaultLinkGenerator CreateLinkGenerator(EndpointDataSource[] dataSources)
        {
            return CreateLinkGenerator(configureServices: null, dataSources);
        }

        private protected DefaultLinkGenerator CreateLinkGenerator(
            Action<IServiceCollection> configureServices,
            EndpointDataSource[] dataSources)
        {
            var services = GetBasicServices();
            AddAdditionalServices(services);
            configureServices?.Invoke(services);

            services.Configure<RouteOptions>(o =>
            {
                if (dataSources != null)
                {
                    foreach (var dataSource in dataSources)
                    {
                        o.EndpointDataSources.Add(dataSource);
                    }
                }
            });

            var serviceProvider = services.BuildServiceProvider();
            var routeOptions = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();

            return new DefaultLinkGenerator(
                new DefaultParameterPolicyFactory(routeOptions, serviceProvider),
                serviceProvider.GetRequiredService<TemplateBinderFactory>(),
                new CompositeEndpointDataSource(routeOptions.Value.EndpointDataSources),
                routeOptions,
                NullLogger<DefaultLinkGenerator>.Instance,
                serviceProvider);
        }
    }
}
