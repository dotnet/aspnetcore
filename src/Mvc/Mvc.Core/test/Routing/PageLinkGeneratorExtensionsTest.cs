// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Routing;

public class PageLinkGeneratorExtensionsTest
{
    [Fact]
    public void GetPathByPage_WithHttpContext_PromotesAmbientValues()
    {
        // Arrange
        var endpoint1 = CreateEndpoint(
            "About/{id}",
            defaults: new { page = "/About", },
            requiredValues: new { page = "/About", });
        var endpoint2 = CreateEndpoint(
            "Admin/ManageUsers/{handler?}",
            defaults: new { page = "/Admin/ManageUsers", },
            requiredValues: new { page = "/Admin/ManageUsers", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext(new { page = "/About", id = 17, });
        httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

        // Act
        var path = linkGenerator.GetPathByPage(
            httpContext,
            values: new RouteValueDictionary(new { id = 18, query = "some?query" }),
            fragment: new FragmentString("#Fragment?"),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("/Foo/Bar%3Fencodeme%3F/About/18/?query=some%3Fquery#Fragment?", path);
    }

    [Fact]
    public void GetPathByPage_WithoutHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = CreateEndpoint(
            "About/{id}",
            defaults: new { page = "/About", },
            requiredValues: new { page = "/About", });
        var endpoint2 = CreateEndpoint(
            "Admin/ManageUsers/{handler?}",
            defaults: new { page = "/Admin/ManageUsers", },
            requiredValues: new { page = "/Admin/ManageUsers", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetPathByPage(
            page: "/Admin/ManageUsers",
            handler: "Delete",
            values: new RouteValueDictionary(new { user = "jamesnk", query = "some?query" }),
            new PathString("/Foo/Bar?encodeme?"),
            new FragmentString("#Fragment?"),
            new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("/Foo/Bar%3Fencodeme%3F/Admin/ManageUsers/Delete/?user=jamesnk&query=some%3Fquery#Fragment?", path);
    }

    [Fact]
    public void GetPathByPage_WithHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = CreateEndpoint(
            "About/{id}",
            defaults: new { page = "/About", },
            requiredValues: new { page = "/About", });
        var endpoint2 = CreateEndpoint(
            "Admin/ManageUsers",
            defaults: new { page = "/Admin/ManageUsers", },
            requiredValues: new { page = "/Admin/ManageUsers", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext(new { page = "/Admin/ManageUsers", handler = "DeleteUser", });
        httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

        // Act
        var path = linkGenerator.GetPathByPage(
            httpContext,
            page: "/About",
            values: new RouteValueDictionary(new { id = 19, query = "some?query" }),
            fragment: new FragmentString("#Fragment?"),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("/Foo/Bar%3Fencodeme%3F/About/19/?query=some%3Fquery#Fragment?", path);
    }

    [Fact]
    public void GetUriByPage_WithoutHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = CreateEndpoint(
            "About/{id}",
            defaults: new { page = "/About", },
            requiredValues: new { page = "/About", });
        var endpoint2 = CreateEndpoint(
            "Admin/ManageUsers",
            defaults: new { page = "/Admin/ManageUsers", },
            requiredValues: new { page = "/Admin/ManageUsers", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        // Act
        var path = linkGenerator.GetUriByPage(
            page: "/About",
            handler: null,
            values: new RouteValueDictionary(new { id = 19, query = "some?query" }),
            "http",
            new HostString("example.com"),
            new PathString("/Foo/Bar?encodeme?"),
            new FragmentString("#Fragment?"),
            new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/About/19/?query=some%3Fquery#Fragment?", path);
    }

    [Fact]
    public void GetUriByPage_WithHttpContext_WithPathBaseAndFragment()
    {
        // Arrange
        var endpoint1 = CreateEndpoint(
            "About/{id}",
            defaults: new { page = "/About", },
            requiredValues: new { page = "/About", });
        var endpoint2 = CreateEndpoint(
            "Admin/ManageUsers",
            defaults: new { page = "/Admin/ManageUsers", },
            requiredValues: new { page = "/Admin/ManageUsers", });

        var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

        var httpContext = CreateHttpContext(new { page = "/Admin/ManageUsers", });
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");
        httpContext.Request.PathBase = new PathString("/Foo/Bar?encodeme?");

        // Act
        var uri = linkGenerator.GetUriByPage(
            httpContext,
            values: new RouteValueDictionary(new { query = "some?query" }),
            fragment: new FragmentString("#Fragment?"),
            options: new LinkOptions() { AppendTrailingSlash = true, });

        // Assert
        Assert.Equal("http://example.com/Foo/Bar%3Fencodeme%3F/Admin/ManageUsers/?query=some%3Fquery#Fragment?", uri);
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
