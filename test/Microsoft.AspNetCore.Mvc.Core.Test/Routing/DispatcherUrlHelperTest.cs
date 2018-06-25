// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class DispatcherUrlHelperTest : UrlHelperTestBase
    {
        protected override IUrlHelper CreateUrlHelper(string appRoot, string host, string protocol)
        {
            return CreateUrlHelper(Enumerable.Empty<MatcherEndpoint>(), appRoot, host, protocol);
        }

        protected override IUrlHelper CreateUrlHelperWithDefaultRoutes(string appRoot, string host, string protocol)
        {
            return CreateUrlHelper(GetDefaultEndpoints(), appRoot, host, protocol);
        }

        protected override IUrlHelper CreateUrlHelperWithDefaultRoutes(
            string appRoot,
            string host,
            string protocol,
            string routeName,
            string template)
        {
            var endpoints = GetDefaultEndpoints();
            endpoints.Add(new MatcherEndpoint(
                next => httpContext => Task.CompletedTask,
                template,
                new RouteValueDictionary(),
                new RouteValueDictionary(),
                0,
                EndpointMetadataCollection.Empty,
                null));
            return CreateUrlHelper(endpoints, appRoot, host, protocol);
        }

        protected override IUrlHelper CreateUrlHelper(ActionContext actionContext)
        {
            var httpContext = actionContext.HttpContext;
            httpContext.Features.Set<IEndpointFeature>(new EndpointFeature()
            {
                Endpoint = new MatcherEndpoint(
                    next => cntxt => Task.CompletedTask,
                    "/",
                    new RouteValueDictionary(),
                    new RouteValueDictionary(),
                    0,
                    EndpointMetadataCollection.Empty,
                    null)
            });

            var urlHelperFactory = httpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);
            Assert.IsType<DispatcherUrlHelper>(urlHelper);
            return urlHelper;
        }

        protected override IServiceProvider CreateServices()
        {
            return CreateServices(Enumerable.Empty<Endpoint>());
        }

        protected override IUrlHelper CreateUrlHelper(
            string appRoot,
            string host,
            string protocol,
            string routeName,
            string template,
            object defaults)
        {
            var endpoint = GetEndpoint(routeName, template, new RouteValueDictionary(defaults));
            var services = CreateServices(new[] { endpoint });
            var httpContext = CreateHttpContext(services, appRoot: "", host: null, protocol: null);
            var actionContext = CreateActionContext(httpContext);
            return CreateUrlHelper(actionContext);
        }

        private IUrlHelper CreateUrlHelper(
            IEnumerable<MatcherEndpoint> endpoints,
            string appRoot,
            string host,
            string protocol)
        {
            var serviceProvider = CreateServices(endpoints);
            var httpContext = CreateHttpContext(serviceProvider, appRoot, host, protocol);
            var actionContext = CreateActionContext(httpContext);
            return CreateUrlHelper(actionContext);
        }

        private List<MatcherEndpoint> GetDefaultEndpoints()
        {
            var endpoints = new List<MatcherEndpoint>();
            endpoints.Add(CreateEndpoint(null, "home/newaction/{id?}", new { id = "defaultid", controller = "home", action = "newaction" }, 1));
            endpoints.Add(CreateEndpoint(null, "home/contact/{id?}", new { id = "defaultid", controller = "home", action = "contact" }, 2));
            endpoints.Add(CreateEndpoint(null, "home2/newaction/{id?}", new { id = "defaultid", controller = "home2", action = "newaction" }, 3));
            endpoints.Add(CreateEndpoint(null, "home2/contact/{id?}", new { id = "defaultid", controller = "home2", action = "contact" }, 4));
            endpoints.Add(CreateEndpoint(null, "home3/contact/{id?}", new { id = "defaultid", controller = "home3", action = "contact" }, 5));
            endpoints.Add(CreateEndpoint("namedroute", "named/home/newaction/{id?}", new { id = "defaultid", controller = "home", action = "newaction" }, 6));
            endpoints.Add(CreateEndpoint("namedroute", "named/home2/newaction/{id?}", new { id = "defaultid", controller = "home2", action = "newaction" }, 7));
            endpoints.Add(CreateEndpoint("namedroute", "named/home/contact/{id?}", new { id = "defaultid", controller = "home", action = "contact" }, 8));
            endpoints.Add(CreateEndpoint("MyRouteName", "any/url", new { }, 9));
            return endpoints;
        }

        private MatcherEndpoint CreateEndpoint(string routeName, string template, object defaults, int order)
        {
            var metadata = EndpointMetadataCollection.Empty;
            if (!string.IsNullOrEmpty(routeName))
            {
                metadata = new EndpointMetadataCollection(new[] { new RouteNameMetadata(routeName) });
            }

            return new MatcherEndpoint(
                next => (httpContext) => Task.CompletedTask,
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(),
                order,
                metadata,
                "DisplayName");
        }

        private IServiceProvider CreateServices(IEnumerable<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                endpoints = Enumerable.Empty<Endpoint>();
            }

            var services = GetCommonServices();
            services.AddDispatcher();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<EndpointDataSource>(new DefaultEndpointDataSource(endpoints)));
            services.TryAddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            return services.BuildServiceProvider();
        }

        private MatcherEndpoint GetEndpoint(string name, string template, RouteValueDictionary defaults)
        {
            return new MatcherEndpoint(
                next => c => Task.CompletedTask,
                template,
                defaults,
                new RouteValueDictionary(),
                0,
                EndpointMetadataCollection.Empty,
                null);
        }

        private class RouteNameMetadata : IRouteNameMetadata
        {
            public RouteNameMetadata(string routeName)
            {
                Name = routeName;
            }

            public string Name { get; }
        }
    }
}
