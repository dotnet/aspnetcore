// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

// Integration tests for GetXyzByRouteValues. These are basic because important behavioral details
// are covered elsewhere.
//
// Does not cover template processing in detail, those scenarios are validated by TemplateBinderTests
// and DefaultLinkGeneratorProcessTemplateTest
//
// Does not cover the RouteValuesAddressScheme in detail. see RouteValuesAddressSchemeTest
public class LinkGeneratorRouteValuesAddressExtensionsTest : LinkGeneratorTestBase
{
    [Fact]
    public void GetPathByRouteValues_WithHttpContext_UsesAmbientValues()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary(new { action = "Index", });
        httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

        // Act
        var path = linkGenerator.GetPathByRouteValues(
            httpContext,
            routeName: null,
            values: new RouteValueDictionary(new { controller = "Home", query = "some?query" }),
            fragment: new FragmentString("#Fragment?"),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", path);
    }

    [Fact]
    public void GetPathByRouteValues_WithoutHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetPathByRouteValues(
            routeName: null,
            values: new RouteValueDictionary(new { controller = "Home", action = "Index", query = "some?query" }),
            new PathString("/Foo/Bar?encodeme?"),
            new FragmentString("#Fragment?"),
            new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", path);
    }

    [Fact]
    public void GetPathByRouteValues_WithHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

        // Act
        var path = linkGenerator.GetPathByRouteValues(
            httpContext,
            routeName: null,
            values: new RouteValueDictionary(new { controller = "Home", action = "Index", query = "some?query" }),
            fragment: new FragmentString("#Fragment?"),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", path);
    }

    [Fact]
    public void GetUriByRouteValues_WithoutHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetUriByRouteValues(
            routeName: null,
            values: new RouteValueDictionary(new { controller = "Home", action = "Index", query = "some?query" }),
            "http",
            new HostString("example.com"),
            new PathString("/Foo/Bar?encodeme?"),
            new FragmentString("#Fragment?"),
            new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", path);
    }

    [Fact]
    public void GetUriByRouteValues_WithHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");
        httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

        // Act
        var uri = linkGenerator.GetUriByRouteValues(
            httpContext,
            routeName: null,
            values: new RouteValueDictionary(new { controller = "Home", action = "Index", query = "some?query" }),
            fragment: new FragmentString("#Fragment?"),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", uri);
    }

    [Fact]
    public void GetUriByRouteValues_WithHttpContext_CanUseAmbientValues()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint(
            "Home/Index/{id?}",
            defaults: new { controller = "Home", action = "Index", },
            requiredValues: new { controller = "Home", action = "Index", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext(new { controller = "Home", });
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");
        httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

        // Act
        var uri = linkGenerator.GetUriByRouteValues(
            httpContext,
            routeName: null,
            values: new RouteValueDictionary(new { action = "Index", query = "some?query" }),
            fragment: new FragmentString("#Fragment?"),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Home/Index/?query=some%3Fquery#Fragment?", uri);
    }
}
