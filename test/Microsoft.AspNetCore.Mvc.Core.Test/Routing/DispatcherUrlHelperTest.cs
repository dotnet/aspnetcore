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
                null,
                0,
                EndpointMetadataCollection.Empty,
                null,
                new Address(routeName)
                ));
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
                    new { },
                    0,
                    EndpointMetadataCollection.Empty,
                    null,
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
            var endpoint = GetEndpoint(routeName, template, defaults);
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
            endpoints.Add(new MatcherEndpoint(
                next => (httpContext) => Task.CompletedTask,
                "{controller}/{action}/{id}",
                new { id = "defaultid" },
                0,
                EndpointMetadataCollection.Empty,
                "RouteWithNoName",
                address: null));
            endpoints.Add(new MatcherEndpoint(
                next => (httpContext) => Task.CompletedTask,
                "named/{controller}/{action}/{id}",
                new { id = "defaultid" },
                0,
                EndpointMetadataCollection.Empty,
                "RouteWithNoName",
                new Address("namedroute")));
            return endpoints;
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

        private MatcherEndpoint GetEndpoint(string name, string template, object defaults)
        {
            return new MatcherEndpoint(
                next => c => Task.CompletedTask,
                template,
                defaults,
                0,
                EndpointMetadataCollection.Empty,
                null,
                new Address(name));
        }
    }
}
