// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Routing;

public class ControllerLinkGeneratorExtensionsTest
{
    [Fact]
    public void GetUriByAction_WhenRequiredAttributeIsNull_Throws()
    {
        var endpoint1 = CreateEndpoint(
           "Home/Index/{id}",
           defaults: new { controller = "Home", action = "Index", },
           requiredValues: new { controller = "Home", action = "Index" });

        var linkGenerator = CreateLinkGenerator(endpoint1);

        // Act
        var exception = Assert.Throws<ArgumentException>(() =>
                    linkGenerator.GetUriByAction("Index", "Home", null, "http", default));
        Assert.Equal("host", exception.ParamName);

        exception = Assert.Throws<ArgumentNullException>(() =>
                    linkGenerator.GetUriByAction((string)null, "Home", null, null, new("localhost")));
        Assert.Equal("action", exception.ParamName);

        exception = Assert.Throws<ArgumentNullException>(() =>
                    linkGenerator.GetUriByAction("Index", null, null, null, new("localhost")));
        Assert.Equal("controller", exception.ParamName);

        exception = Assert.Throws<ArgumentNullException>(() =>
                    linkGenerator.GetUriByAction("Index", "Home", null, null, new("localhost")));
        Assert.Equal("scheme", exception.ParamName);
    }

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
