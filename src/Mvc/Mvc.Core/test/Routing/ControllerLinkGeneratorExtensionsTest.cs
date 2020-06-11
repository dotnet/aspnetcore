// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class ControllerLinkGeneratorExtensionsTest
    {
        [Fact]
        public void GetPathByAction_WithHttpContext_PromotesAmbientValues()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "Home/Index/{id}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });
            var endpoint2 = CreateEndpoint(
                "Home/Index/{id?}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext(new { controller = "Home", });
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            // Act
            var path = linkGenerator.GetPathByAction(
                httpContext,
                action: "Index",
                values: new RouteValueDictionary(new { query = "some?query" }),
                fragment: new FragmentString("#Fragment?"),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetPathByAction_WithoutHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "Home/Index/{id}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });
            var endpoint2 = CreateEndpoint(
                "Home/Index/{id?}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetPathByAction(
                action: "Index",
                controller: "Home",
                values: new RouteValueDictionary(new { query = "some?query" }),
                new PathString("/Foo/Bar?encodeme?"),
                new FragmentString("#Fragment?"),
                new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetPathByAction_WithHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "Home/Index/{id}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });
            var endpoint2 = CreateEndpoint(
                "Home/Index/{id?}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            // Act
            var path = linkGenerator.GetPathByAction(
                httpContext,
                action: "Index",
                controller: "Home",
                values: new RouteValueDictionary(new { query = "some?query" }),
                fragment: new FragmentString("#Fragment?"),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetUriByAction_WithoutHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "Home/Index/{id}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });
            var endpoint2 = CreateEndpoint(
                "Home/Index/{id?}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetUriByAction(
                action: "Index",
                controller: "Home",
                values: new RouteValueDictionary(new { query = "some?query" }),
                "http",
                new HostString("example.com"),
                new PathString("/Foo/Bar?encodeme?"),
                new FragmentString("#Fragment?"),
                new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetUriByAction_WithHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "Home/Index/{id}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });
            var endpoint2 = CreateEndpoint(
                "Home/Index/{id?}",
                defaults: new { controller = "Home", action = "Index", },
                requiredValues: new { controller = "Home", action = "Index" });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext(new { controller = "Home", action = "Index", });
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            // Act
            var uri = linkGenerator.GetUriByAction(
                httpContext,
                values: new RouteValueDictionary(new { query = "some?query" }),
                fragment: new FragmentString("#Fragment?"),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", uri);
        }

        private RouteEndpoint CreateEndpoint(
            string template,
            object defaults = null,
            object requiredValues = null,
            int order = 0,
            object[] metadata = null)
        {
            return new RouteEndpoint(
                (httpContext) => Task.CompletedTask,
                RoutePatternFactory.Parse(template, defaults, parameterPolicies: null, requiredValues),
                order,
                new EndpointMetadataCollection(metadata ?? Array.Empty<object>()),
                null);
        }

        private IServiceProvider CreateServices(IEnumerable<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                endpoints = Enumerable.Empty<Endpoint>();
            }

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();
            services.AddRouting();
            services
                .AddSingleton<UrlEncoder>(UrlEncoder.Default);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<EndpointDataSource>(new DefaultEndpointDataSource(endpoints)));
            return services.BuildServiceProvider();
        }

        private LinkGenerator CreateLinkGenerator(params Endpoint[] endpoints)
        {
            var services = CreateServices(endpoints);
            return services.GetRequiredService<LinkGenerator>();
        }

        private HttpContext CreateHttpContext(object ambientValues = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues = new RouteValueDictionary(ambientValues);
            return httpContext;
        }
    }
}