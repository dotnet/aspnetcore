// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    // Integration tests for GetXyzByName. These are basic because important behavioral details
    // are covered elsewhere.
    //
    // Does not cover template processing in detail, those scenarios are validated by TemplateBinderTests
    // and DefaultLinkGeneratorProcessTemplateTest
    //
    // Does not cover the EndpointNameAddressScheme in detail. see EndpointNameAddressSchemeTest
    public class LinkGeneratorEndpointNameExtensionsTest : LinkGeneratorTestBase
    {
        [Fact]
        public void GetPathByName_WithHttpContext_DoesNotUseAmbientValues()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("some-endpoint/{p}", metadata: new[] { new EndpointNameMetadata("name1"), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("some#-other-endpoint/{p}", metadata: new[] { new EndpointNameMetadata("name2"), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var context = new EndpointSelectorContext()
            {
                RouteValues = new RouteValueDictionary(new { p = "5", })
            };
            var httpContext = CreateHttpContext();
            httpContext.Features.Set<IRouteValuesFeature>(context);
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            var values = new { query = "some?query", };

            // Act
            var path = linkGenerator.GetPathByName(
                httpContext,
                endpointName: "name2",
                values,
                fragment: new FragmentString("#Fragment?"),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void GetPathByName_WithoutHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("some-endpoint/{p}",  metadata: new[] { new EndpointNameMetadata("name1"), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("some#-other-endpoint/{p}", metadata: new[] { new EndpointNameMetadata("name2"), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var values = new { p = "In?dex", query = "some?query", };

            // Act
            var path = linkGenerator.GetPathByName(
                endpointName: "name2",
                values,
                new PathString("/Foo/Bar?encodeme?"),
                new FragmentString("#Fragment?"),
                new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("/Foo/Bar%3Fencodeme%3F/some%23-other-endpoint/In%3Fdex/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetPathByName_WithHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("some-endpoint/{p}", metadata: new[] { new EndpointNameMetadata("name1"), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("some#-other-endpoint/{p}", metadata: new[] { new EndpointNameMetadata("name2"), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            var values = new { p = "In?dex", query = "some?query", };

            // Act
            var path = linkGenerator.GetPathByName(
                httpContext,
                endpointName: "name2",
                values,
                fragment: new FragmentString("#Fragment?"),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("/Foo/Bar%3Fencodeme%3F/some%23-other-endpoint/In%3Fdex/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetUriByRouteValues_WithoutHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("some-endpoint/{p}", metadata: new[] { new EndpointNameMetadata("name1"), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("some#-other-endpoint/{p}", metadata: new[] { new EndpointNameMetadata("name2"), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var values = new { p = "In?dex", query = "some?query", };

            // Act
            var path = linkGenerator.GetUriByName(
                endpointName: "name2",
                values,
                "http",
                new HostString("example.com"),
                new PathString("/Foo/Bar?encodeme?"),
                new FragmentString("#Fragment?"),
                new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/some%23-other-endpoint/In%3Fdex/?query=some%3Fquery#Fragment?", path);
        }

        [Fact]
        public void GetUriByName_WithHttpContext_WithPathBaseAndFragment()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateRouteEndpoint("some-endpoint/{p}", metadata: new[] { new EndpointNameMetadata("name1"), });
            var endpoint2 = EndpointFactory.CreateRouteEndpoint("some#-other-endpoint/{p}", metadata: new[] { new EndpointNameMetadata("name2"), });

            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            var httpContext = CreateHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("example.com");
            httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

            var values = new { p = "In?dex", query = "some?query", };

            // Act
            var uri = linkGenerator.GetUriByName(
                httpContext,
                endpointName: "name2",
                values,
                fragment: new FragmentString("#Fragment?"),
                options: new LinkOptions() { AppendTrailingSlash = true, });

            // Assert
            Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/some%23-other-endpoint/In%3Fdex/?query=some%3Fquery#Fragment?", uri);
        }
    }
}
