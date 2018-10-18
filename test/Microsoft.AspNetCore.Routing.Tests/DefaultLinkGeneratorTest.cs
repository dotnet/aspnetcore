// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    // Tests LinkGenerator functionality using GetXyzByAddress - see tests for the extension
    // methods for more E2E tests.
    //
    // Does not cover template processing in detail, those scenarios are validated by TemplateBinderTests
    // and DefaultLinkGeneratorProcessTemplateTest
    public class DefaultLinkGeneratorTest : LinkGeneratorTestBase
    {
        [Fact]
        public void GetPathByAddress_WithoutHttpContext_NoMatches_ReturnsNull()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint);

            // Act
            var path = linkGenerator.GetPathByAddress(0, values: null);

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void GetPathByAddress_WithHttpContext_NoMatches_ReturnsNull()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint);

            // Act
            var path = linkGenerator.GetPathByAddress(CreateHttpContext(), 0, values: null);

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void GetUriByAddress_WithoutHttpContext_NoMatches_ReturnsNull()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint);

            // Act
            var uri = linkGenerator.GetUriByAddress(0, values: null, "http", new HostString("example.com"));

            // Assert
            Assert.Null(uri);
        }

        [Fact]
        public void GetUriByAddress_WithHttpContext_NoMatches_ReturnsNull()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint);

            // Act
            var uri = linkGenerator.GetUriByAddress(CreateHttpContext(), 0, values: null);

            // Assert
            Assert.Null(uri);
        }

        [Fact]
        public void GetPathByAddress_WithoutHttpContext_HasMatches_ReturnsFirstSuccessfulTemplateResult()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetPathByAddress(1, values: new RouteValueDictionary(new { controller = "Home", action = "Index", }));

            // Assert
            Assert.Equal("/Home/Index", path);
        }

        [Fact]
        public void GetPathByAddress_WithHttpContext_HasMatches_ReturnsFirstSuccessfulTemplateResult()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetPathByAddress(CreateHttpContext(), 1, values: new RouteValueDictionary(new { controller = "Home", action = "Index", }));

            // Assert
            Assert.Equal("/Home/Index", path);
        }

        [Fact]
        public void GetUriByAddress_WithoutHttpContext_HasMatches_ReturnsFirstSuccessfulTemplateResult()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetUriByAddress(
                1,
                values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
                "http",
                new HostString("example.com"));

            // Assert
            Assert.Equal("http://example.com/Home/Index", path);
        }

        [Fact]
        public void GetUriByAddress_WithHttpContext_HasMatches_ReturnsFirstSuccessfulTemplateResult()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");

            // Act
            var uri = linkGenerator.GetUriByAddress(httpContext, 1, values: new RouteValueDictionary(new { controller = "Home", action = "Index", }));

            // Assert
            Assert.Equal("http://example.com/Home/Index", uri);
        }

        [Fact]
        public void GetPathByAddress_WithoutHttpContext_WithLinkOptions()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetPathByAddress(
                1,
                values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("/Home/Index/", path);
        }

        [Fact]
        public void GetPathByAddress_WithParameterTransformer()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller:slugify}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller:slugify}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var routeOptions = new RouteOptions();
            routeOptions.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);

            var linkGenerator = CreateLinkGenerator(routeOptions: routeOptions, configureServices: null, endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetPathByAddress(
                1,
                values: new RouteValueDictionary(new { controller = "TestController", action = "Index", }));

            // Assert
            Assert.Equal("/test-controller/Index", path);
        }

        [Fact]
        public void GetPathByAddress_WithParameterTransformer_WithLowercaseUrl()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller:slugify}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller:slugify}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var routeOptions = new RouteOptions();
            routeOptions.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);

            var linkGenerator = CreateLinkGenerator(routeOptions: routeOptions, configureServices: null, endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetPathByAddress(
                1,
                values: new RouteValueDictionary(new { controller = "TestController", action = "Index", }),
                options: new LinkOptions() { LowercaseUrls = true, });

            // Assert
            Assert.Equal("/test-controller/index", path);
        }

        [Fact]
        public void GetPathByAddress_WithHttpContext_WithLinkOptions()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetPathByAddress(
                CreateHttpContext(),
                1,
                values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("/Home/Index/", path);
        }

        [Fact]
        public void GetUriByAddress_WithoutHttpContext_WithLinkOptions()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetUriByAddress(
                1,
                values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
                "http",
                new HostString("example.com"),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("http://example.com/Home/Index/", path);
        }

        [Fact]
        public void GetUriByAddress_WithHttpContext_WithLinkOptions()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");

            // Act
            var uri = linkGenerator.GetUriByAddress(
                httpContext,
                1,
                values: new RouteValueDictionary(new { controller = "Home", action = "Index", }),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("http://example.com/Home/Index/", uri);
        }

        // Includes characters that need to be encoded
        [Fact]
        public void GetPathByAddress_WithoutHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetPathByAddress(
                1,
                values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
                new PathString("/Foo/Bar?encodeme?"),
                new FragmentString("#Fragment?"));

            // Assert
            Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex?query=some%3Fquery#Fragment?", path);
        }

        private class UpperCaseParameterTransform : IOutboundParameterTransformer
        {
            public string TransformOutbound(object value)
            {
                return value?.ToString()?.ToUpperInvariant();
            }
        }

        [Fact]
        public void GetLink_ParameterTransformer()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint("{controller:upper-case}/{name}");

            var routeOptions = new RouteOptions();
            routeOptions.ConstraintMap["upper-case"] = typeof(UpperCaseParameterTransform);

            Action<IServiceCollection> configure = (s) =>
            {
                s.AddSingleton(typeof(UpperCaseParameterTransform), new UpperCaseParameterTransform());
            };

            var linkGenerator = CreateLinkGenerator(routeOptions, configure, endpoint);

            // Act
            var link = linkGenerator.GetPathByRouteValues(routeName: null, new { controller = "Home", name = "Test" });

            // Assert
            Assert.Equal("/HOME/Test", link);
        }

        [Fact]
        public void GetLink_ParameterTransformer_ForQueryString()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint("{controller:upper-case}/{name}", policies: new { c = new UpperCaseParameterTransform(), });

            var routeOptions = new RouteOptions();
            routeOptions.ConstraintMap["upper-case"] = typeof(UpperCaseParameterTransform);

            Action<IServiceCollection> configure = (s) =>
            {
                s.AddSingleton(typeof(UpperCaseParameterTransform), new UpperCaseParameterTransform());
            };

            var linkGenerator = CreateLinkGenerator(routeOptions, configure, endpoint);

            // Act
            var link = linkGenerator.GetPathByRouteValues(routeName: null, new { controller = "Home", name = "Test", c = "hithere", });

            // Assert
            Assert.Equal("/HOME/Test?c=HITHERE", link);
        }

        // Includes characters that need to be encoded
        [Fact]
        public void GetPathByAddress_WithHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            // Act
            var path = linkGenerator.GetPathByAddress(
                httpContext,
                1,
                values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
                fragment: new FragmentString("#Fragment?"));

            // Assert
            Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex?query=some%3Fquery#Fragment?", path);
        }

        // Includes characters that need to be encoded
        [Fact]
        public void GetUriByAddress_WithoutHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var path = linkGenerator.GetUriByAddress(
                1,
                values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
                "http",
                new HostString("example.com"),
                new PathString("/Foo/Bar?encodeme?"),
                new FragmentString("#Fragment?"));

            // Assert
            Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex?query=some%3Fquery#Fragment?", path);
        }

        // Includes characters that need to be encoded
        [Fact]
        public void GetUriByAddress_WithHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            // Act
            var uri = linkGenerator.GetUriByAddress(
                httpContext,
                1,
                values: new RouteValueDictionary(new { controller = "Home", action = "In?dex", query = "some?query" }),
                fragment: new FragmentString("#Fragment?"));

            // Assert
            Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/In%3Fdex?query=some%3Fquery#Fragment?", uri);
        }

        [Fact]
        public void GetPathByAddress_WithHttpContext_IncludesAmbientValues()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");

            // Act
            var uri = linkGenerator.GetPathByAddress(
                httpContext, 
                1, 
                values: new RouteValueDictionary(new { action = "Index", }), 
                ambientValues: new RouteValueDictionary(new { controller = "Home", }));

            // Assert
            Assert.Equal("/Home/Index", uri);
        }

        [Fact]
        public void GetUriByAddress_WithHttpContext_IncludesAmbientValues()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");

            // Act
            var uri = linkGenerator.GetUriByAddress(
                httpContext, 
                1, 
                values: new RouteValueDictionary(new { action = "Index", }),
                ambientValues: new RouteValueDictionary(new { controller = "Home", }));

            // Assert
            Assert.Equal("http://example.com/Home/Index", uri);
        }

        [Fact]
        public void GetPathByAddress_WithHttpContext_CanOverrideUriParts()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.PathBase = "/Foo";

            // Act
            var uri = linkGenerator.GetPathByAddress(
                httpContext,
                1,
                values: new RouteValueDictionary(new { action = "Index", controller= "Home", }),
                pathBase: "/");

            // Assert
            Assert.Equal("/Home/Index", uri);
        }

        [Fact]
        public void GetUriByAddress_WithHttpContext_CanOverrideUriParts()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");
            httpContext.Request.PathBase = "/Foo";

            // Act
            var uri = linkGenerator.GetUriByAddress(
                httpContext,
                1,
                values: new RouteValueDictionary(new { action = "Index", controller = "Home", }),
                scheme: "ftp",
                host: new HostString("example.com:5000"),
                pathBase: "/");

            // Assert
            Assert.Equal("ftp://example.com:5000/Home/Index", uri);
        }

        [Fact]
        public void GetTemplateByAddress_WithNoMatch_ReturnsNull()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var template = linkGenerator.GetTemplateByAddress(address: 0);

            // Assert
            Assert.Null(template);
        }

        [Fact]
        public void GetTemplateByAddress_WithMatch_ReturnsTemplate()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new IntMetadata(1), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var template = linkGenerator.GetTemplateByAddress(address: 1);

            // Assert
            Assert.NotNull(template);
            Assert.Collection(
                Assert.IsType<DefaultLinkGenerationTemplate>(template).Endpoints,
                e => Assert.Same(endpoint1, e),
                e => Assert.Same(endpoint2, e));
        }

        [Fact]
        public void GetTemplateBinder_CanCache()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var dataSource = new DynamicEndpointDataSource(endpoint1);

            var linkGenerator = CreateLinkGenerator(dataSources: new[] { dataSource });

            var expected = linkGenerator.GetTemplateBinder(endpoint1);

            // Act
            var actual = linkGenerator.GetTemplateBinder(endpoint1);

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void GetTemplateBinder_CanClearCache()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            var dataSource = new DynamicEndpointDataSource(endpoint1);

            var linkGenerator = CreateLinkGenerator(dataSources: new[] { dataSource });
            var original = linkGenerator.GetTemplateBinder(endpoint1);

            var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
            dataSource.AddEndpoint(endpoint2);

            // Act
            var actual = linkGenerator.GetTemplateBinder(endpoint1);

            // Assert
            Assert.NotSame(original, actual);
        }

        [Fact]
        public void GetPathByRouteValues_UsesFirstTemplateThatSucceeds()
        {
            // Arrange
            var endpointControllerAction = EndpointFactory.CreateRouteEndpoint(
                "Home/Index",
                order: 3,
                defaults: new { controller = "Home", action = "Index", },
                metadata: new[] { new RouteValuesAddressMetadata(new RouteValueDictionary(new { controller = "Home", action = "Index", })) });
            var endpointController = EndpointFactory.CreateRouteEndpoint(
                "Home",
                order: 2,
                defaults: new { controller = "Home", action = "Index", },
                metadata: new[] { new RouteValuesAddressMetadata(new RouteValueDictionary(new { controller = "Home", action = "Index", })) });
            var endpointEmpty = EndpointFactory.CreateRouteEndpoint(
                "",
                order: 1,
                defaults: new { controller = "Home", action = "Index", },
                metadata: new[] { new RouteValuesAddressMetadata(new RouteValueDictionary(new { controller = "Home", action = "Index", })) });

            // This endpoint should be used to generate the link when an id is present
            var endpointControllerActionParameter = EndpointFactory.CreateRouteEndpoint(
                "Home/Index/{id}",
                order: 0,
                defaults: new { controller = "Home", action = "Index", },
                metadata: new[] { new RouteValuesAddressMetadata(new RouteValueDictionary(new { controller = "Home", action = "Index", })) });

            var linkGenerator = CreateLinkGenerator(endpointControllerAction, endpointController, endpointEmpty, endpointControllerActionParameter);

            var context = new EndpointSelectorContext()
            {
                RouteValues = new RouteValueDictionary(new { controller = "Home", action = "Index", })
            };
            var httpContext = CreateHttpContext();
            httpContext.Features.Set<IRouteValuesFeature>(context);

            // Act
            var pathWithoutId = linkGenerator.GetPathByRouteValues(
                httpContext,
                routeName: null,
                values: new RouteValueDictionary());

            var pathWithId = linkGenerator.GetPathByRouteValues(
                httpContext,
                routeName: null,
                values: new RouteValueDictionary(new { id = "3" }));

            var pathWithCustom = linkGenerator.GetPathByRouteValues(
                httpContext,
                routeName: null,
                values: new RouteValueDictionary(new { custom = "Custom" }));

            // Assert
            Assert.Equal("/", pathWithoutId);
            Assert.Equal("/Home/Index/3", pathWithId);
            Assert.Equal("/?custom=Custom", pathWithCustom);
        }

        protected override void AddAdditionalServices(IServiceCollection services)
        {
            services.AddSingleton<IEndpointAddressScheme<int>, IntAddressScheme>();
        }

        private class IntAddressScheme : IEndpointAddressScheme<int>
        {
            private readonly CompositeEndpointDataSource _dataSource;

            public IntAddressScheme(CompositeEndpointDataSource dataSource)
            {
                _dataSource = dataSource;
            }

            public IEnumerable<Endpoint> FindEndpoints(int address)
            {
                return _dataSource.Endpoints.Where(e => e.Metadata.GetMetadata<IntMetadata>().Value == address);
            }
        }

        private class IntMetadata
        {
            public IntMetadata(int value)
            {
                Value = value;
            }
            public int Value { get; }
        }
    }
}
