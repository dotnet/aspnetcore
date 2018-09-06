// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class LinkGeneratorTestBase
    {
        protected HttpContext CreateHttpContext(object ambientValues = null)
        {
            var httpContext = new DefaultHttpContext();

            var feature = new EndpointFeature
            {
                RouteValues = new RouteValueDictionary(ambientValues)
            };

            httpContext.Features.Set<IEndpointFeature>(feature);
            httpContext.Features.Set<IRouteValuesFeature>(feature);
            return httpContext;
        }

        private ServiceCollection GetBasicServices()
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
            return CreateLinkGenerator(routeOptions: null, services: null, endpoints);
        }

        private protected DefaultLinkGenerator CreateLinkGenerator(RouteOptions routeOptions = null, IServiceCollection services = null, params Endpoint[] endpoints)
        {
            if (services == null)
            {
                services = GetBasicServices();
                AddAdditionalServices(services);
            }

            if (endpoints != null || endpoints.Length > 0)
            {
                services.Configure<EndpointOptions>(o =>
                {
                    o.DataSources.Add(new DefaultEndpointDataSource(endpoints));
                });
            }

            routeOptions = routeOptions ?? new RouteOptions();
            var options = Options.Create(routeOptions);
            var serviceProvider = services.BuildServiceProvider();

            return new DefaultLinkGenerator(
                new DefaultParameterPolicyFactory(options, serviceProvider),
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                options,
                NullLogger<DefaultLinkGenerator>.Instance,
                serviceProvider);
        }
    }
}
