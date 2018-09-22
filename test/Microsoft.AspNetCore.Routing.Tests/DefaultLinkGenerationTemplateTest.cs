// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    // Tests DefaultLinkGenerationTemplate functionality - these are pretty light since most of the functionality
    // is a direct subset of DefaultLinkGenerator
    //
    // Does not cover template processing in detail, those scenarios are validated by TemplateBinderTests
    // and DefaultLinkGeneratorProcessTemplateTest
    public class DefaultLinkGenerationTemplateTest : LinkGeneratorTestBase
    {
        [Fact]
        public void GetPath_WithoutHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}");
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}");

            var linkGenerator = CreateLinkGenerator();
            var template = new DefaultLinkGenerationTemplate(linkGenerator, new List<RouteEndpoint>() { endpoint1, endpoint2, }, options: null);

            // Act
            var path = template.GetPath(
                values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
                new PathString("/Foo/Bar?encodeme?"),
                new FragmentString("#Fragment?"),
                new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetPath_WithHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}");
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}");

            var linkGenerator = CreateLinkGenerator();
            var template = new DefaultLinkGenerationTemplate(linkGenerator, new List<RouteEndpoint>() { endpoint1, endpoint2, }, options: null);

            var httpContext = CreateHttpContext();
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            // Act
            var path = template.GetPath(
                httpContext,
                values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
                fragment: new FragmentString("#Fragment?"),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetUri_WithoutHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}");
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}");

            var linkGenerator = CreateLinkGenerator();
            var template = new DefaultLinkGenerationTemplate(linkGenerator, new List<RouteEndpoint>() { endpoint1, endpoint2, }, options: null);

            // Act
            var path = template.GetUri(
                values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
                "http",
                new HostString("example.com"),
                new PathString("/Foo/Bar?encodeme?"),
                new FragmentString("#Fragment?"),
                new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetUri_WithHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}");
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}");

            var linkGenerator = CreateLinkGenerator();
            var template = new DefaultLinkGenerationTemplate(linkGenerator, new List<RouteEndpoint>() { endpoint1, endpoint2, }, options: null);

            var httpContext = CreateHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            // Act
            var uri = template.GetUri(
                httpContext,
                values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
                fragment: new FragmentString("#Fragment?"),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex/?query=some%3Fquery#Fragment?", uri);
        }

        [Fact]
        public void GetPath_WithHttpContext_IncludesAmbientValues_WhenUseAmbientValuesIsTrue()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}");
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}");

            var linkGenerator = CreateLinkGenerator();
            var template = new DefaultLinkGenerationTemplate(linkGenerator, new List<RouteEndpoint>() { endpoint1, endpoint2, }, options: new LinkGenerationTemplateOptions()
            {
                UseAmbientValues = true,
            });

            var httpContext = CreateHttpContext(new { controller = "Home", });
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");

            // Act
            var uri = template.GetPath(httpContext, values: new RouteValueDictionary(new { action = "Index", }));

            // Assert
            Assert.Equal("/Home/Index", uri);
        }

        [Fact]
        public void GetPath_WithHttpContext_ExcludesAmbientValues_WhenUseAmbientValuesIsFalse()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}");
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}");

            var linkGenerator = CreateLinkGenerator();
            var template = new DefaultLinkGenerationTemplate(linkGenerator, new List<RouteEndpoint>() { endpoint1, endpoint2, }, options: new LinkGenerationTemplateOptions()
            {
                UseAmbientValues = false,
            });

            var httpContext = CreateHttpContext(new { controller = "Home", });
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");

            // Act
            var uri = template.GetPath(httpContext, values: new RouteValueDictionary(new { action = "Index", }));

            // Assert
            Assert.Null(uri);
        }

        [Fact]
        public void GetUri_WithHttpContext_IncludesAmbientValues_WhenUseAmbientValuesIsTrue()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}");
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}");

            var linkGenerator = CreateLinkGenerator();
            var template = new DefaultLinkGenerationTemplate(linkGenerator, new List<RouteEndpoint>() { endpoint1, endpoint2, }, options: new LinkGenerationTemplateOptions()
            {
                UseAmbientValues = true,
            });

            var httpContext = CreateHttpContext(new { controller = "Home", });
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");

            // Act
            var uri = template.GetUri(httpContext, values: new { action = "Index", });

            // Assert
            Assert.Equal("http://example.com/Home/Index", uri);
        }

        [Fact]
        public void GetUri_WithHttpContext_ExcludesAmbientValues_WhenUseAmbientValuesIsFalse()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}");
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}");

            var linkGenerator = CreateLinkGenerator();
            var template = new DefaultLinkGenerationTemplate(linkGenerator, new List<RouteEndpoint>() { endpoint1, endpoint2, }, options: new LinkGenerationTemplateOptions()
            {
                UseAmbientValues = false,
            });

            var httpContext = CreateHttpContext(new { controller = "Home", });
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");

            // Act
            var uri = template.GetUri(httpContext, values: new { action = "Index", });

            // Assert
            Assert.Null(uri);
        }
    }
}
