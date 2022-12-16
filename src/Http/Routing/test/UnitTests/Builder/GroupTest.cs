// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Builder;

public class GroupTest
{
    private EndpointDataSource GetEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder)
    {
        return Assert.IsAssignableFrom<EndpointDataSource>(Assert.Single(endpointRouteBuilder.DataSources));
    }

    [Fact]
    public async Task Prefix_CanBeEmpty()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var group = builder.MapGroup("");

        group.MapGet("/{id}", (int id, HttpContext httpContext) =>
        {
            httpContext.Items["id"] = id;
        });

        var dataSource = GetEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        Assert.Equal("HTTP: GET /{id}", endpoint.DisplayName);
        Assert.Equal("/{id}", routeEndpoint.RoutePattern.RawText);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["id"] = "42";

        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal(42, httpContext.Items["id"]);
    }

    [Fact]
    public async Task PrefixWithRouteParameter_CanBeUsed()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var group = builder.MapGroup("/{org}");

        group.MapGet("/{id}", (string org, int id, HttpContext httpContext) =>
        {
            httpContext.Items["org"] = org;
            httpContext.Items["id"] = id;
        });

        var dataSource = GetEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        Assert.Equal("HTTP: GET /{org}/{id}", endpoint.DisplayName);
        Assert.Equal("/{org}/{id}", routeEndpoint.RoutePattern.RawText);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["org"] = "dotnet";
        httpContext.Request.RouteValues["id"] = "42";

        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal("dotnet", httpContext.Items["org"]);
        Assert.Equal(42, httpContext.Items["id"]);
    }

    [Fact]
    public async Task NestedPrefixWithRouteParameters_CanBeUsed()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var group = builder.MapGroup("/{org}").MapGroup("/{id}");

        group.MapGet("/", (string org, int id, HttpContext httpContext) =>
        {
            httpContext.Items["org"] = org;
            httpContext.Items["id"] = id;
        });

        var dataSource = GetEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        Assert.Equal("HTTP: GET /{org}/{id}/", endpoint.DisplayName);
        Assert.Equal("/{org}/{id}/", routeEndpoint.RoutePattern.RawText);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["org"] = "dotnet";
        httpContext.Request.RouteValues["id"] = "42";

        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal("dotnet", httpContext.Items["org"]);
        Assert.Equal(42, httpContext.Items["id"]);
    }

    [Fact]
    public void RepeatedRouteParameter_ThrowsRoutePatternException()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        builder.MapGroup("/{ID}").MapGroup("/{id}").MapGet("/", () => { });

        var ex = Assert.Throws<RoutePatternException>(() => builder.DataSources.Single().Endpoints);

        Assert.Equal("/{ID}/{id}", ex.Pattern);
        Assert.Equal("The route parameter name 'id' appears more than one time in the route template.", ex.Message);
    }

    [Fact]
    public void NullParameters_ThrowsArgumentNullException()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        var ex = Assert.Throws<ArgumentNullException>(() => builder.MapGroup((string)null!));
        Assert.Equal("prefix", ex.ParamName);
        ex = Assert.Throws<ArgumentNullException>(() => builder.MapGroup((RoutePattern)null!));
        Assert.Equal("prefix", ex.ParamName);

        builder = null;

        ex = Assert.Throws<ArgumentNullException>(() => builder!.MapGroup(RoutePatternFactory.Parse("/")));
        Assert.Equal("endpoints", ex.ParamName);
        ex = Assert.Throws<ArgumentNullException>(() => builder!.MapGroup("/"));
        Assert.Equal("endpoints", ex.ParamName);
    }

    [Fact]
    public void RoutePatternInConvention_IncludesFullGroupPrefix()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        var outer = builder.MapGroup("/outer");
        var inner = outer.MapGroup("/inner");
        inner.MapGet("/foo", () => "Hello World!");

        RoutePattern? outerPattern = null;
        RoutePattern? innerPattern = null;

        ((IEndpointConventionBuilder)outer).Add(builder =>
        {
            outerPattern = ((RouteEndpointBuilder)builder).RoutePattern;
        });
        ((IEndpointConventionBuilder)inner).Add(builder =>
        {
            innerPattern = ((RouteEndpointBuilder)builder).RoutePattern;
        });

        var dataSource = GetEndpointDataSource(builder);
        Assert.Single(dataSource.Endpoints);

        Assert.Equal("/outer/inner/foo", outerPattern?.RawText);
        Assert.Equal("/outer/inner/foo", innerPattern?.RawText);
    }

    [Fact]
    public void ServiceProviderInConvention_IsSet()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));

        var group = builder.MapGroup("/group");
        group.MapGet("/foo", () => "Hello World!");

        IServiceProvider? endpointBuilderServiceProvider = null;

        ((IEndpointConventionBuilder)group).Add(builder =>
        {
            endpointBuilderServiceProvider = builder.ApplicationServices;
        });

        var dataSource = GetEndpointDataSource(builder);
        Assert.Single(dataSource.Endpoints);

        Assert.Same(serviceProvider, endpointBuilderServiceProvider);
    }

    [Fact]
    public async Task BuildingEndpointInConvention_Works()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        var group = builder.MapGroup("/group");
        var mapGetCalled = false;

        group.MapGet("/", () =>
        {
            mapGetCalled = true;
        });

        Endpoint? conventionBuiltEndpoint = null;

        ((IEndpointConventionBuilder)group).Add(builder =>
        {
            conventionBuiltEndpoint = builder.Build();
        });

        var dataSource = GetEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var httpContext = new DefaultHttpContext();

        Assert.NotNull(conventionBuiltEndpoint);
        Assert.False(mapGetCalled);
        await conventionBuiltEndpoint!.RequestDelegate!(httpContext);
        Assert.True(mapGetCalled);

        mapGetCalled = false;
        await endpoint.RequestDelegate!(httpContext);
        Assert.True(mapGetCalled);
    }

    [Fact]
    public void ModifyingRoutePatternInConvention_Works()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        var group = builder.MapGroup("/group");
        group.MapGet("/foo", () => "Hello World!");

        ((IEndpointConventionBuilder)group).Add(builder =>
        {
            ((RouteEndpointBuilder)builder).RoutePattern = RoutePatternFactory.Parse("/bar");
        });

        var dataSource = GetEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);

        Assert.Equal("/bar", routeEndpoint.RoutePattern.RawText);
    }

    [Fact]
    public async Task ChangingMostEndpointBuilderPropertiesInConvention_Works()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        var group = builder.MapGroup("/group");
        var mapGetCallCount = 0;
        var replacementCalled = false;

        group.MapGet("/", () =>
        {
            mapGetCallCount++;
        });

        ((IEndpointConventionBuilder)group).Add(builder =>
        {
            var originalRequestDelegate = builder.RequestDelegate!;

            builder.DisplayName = $"Prefixed! {builder.DisplayName}";
            builder.RequestDelegate = async ctx =>
            {
                replacementCalled = true;
                await originalRequestDelegate(ctx);
                await originalRequestDelegate(ctx);
            };

            ((RouteEndpointBuilder)builder).Order = 42;
        });

        var dataSource = GetEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var httpContext = new DefaultHttpContext();

        await endpoint!.RequestDelegate!(httpContext);

        Assert.True(replacementCalled);
        Assert.Equal(2, mapGetCallCount);
        Assert.Equal("Prefixed! HTTP: GET /group/", endpoint.DisplayName);

        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        Assert.Equal(42, routeEndpoint.Order);
    }

    [Fact]
    public void GivenNonRouteEndpoint_ThrowsNotSupportedException()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        var group = builder.MapGroup("/group");
        ((IEndpointRouteBuilder)group).DataSources.Add(new TestCustomEndpintDataSource());

        var dataSource = GetEndpointDataSource(builder);
        var ex = Assert.Throws<NotSupportedException>(() => dataSource.Endpoints);
        Assert.Equal(
            "MapGroup does not support custom Endpoint type 'Microsoft.AspNetCore.Builder.GroupTest+TestCustomEndpoint'. " +
            "Only RouteEndpoints can be grouped.",
            ex.Message);
    }

    [Fact]
    public void OuterGroupMetadata_AddedFirst()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        var outer = builder.MapGroup("/outer");
        var inner = outer.MapGroup("/inner");
        inner.MapGet("/foo", () => "Hello World!").WithMetadata("/foo");

        inner.WithMetadata("/inner");
        outer.WithMetadata("/outer");

        var dataSource = GetEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        Assert.Equal(new[] { "/outer", "/inner", "/foo" }, endpoint.Metadata.GetOrderedMetadata<string>());
    }

    [Fact]
    public void MultipleEndpoints_AreSupported()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        var group = builder.MapGroup("/group");
        group.MapGet("/foo", () => "foo");
        group.MapGet("/bar", () => "bar");

        group.WithMetadata("/group");

        var dataSource = GetEndpointDataSource(builder);
        Assert.Collection(dataSource.Endpoints.OfType<RouteEndpoint>(),
            routeEndpoint =>
            {
                Assert.Equal("/group/foo", routeEndpoint.RoutePattern.RawText);
                Assert.True(routeEndpoint.Metadata.Count >= 1);
                Assert.Equal("/group", routeEndpoint.Metadata.GetMetadata<string>());
            },
            routeEndpoint =>
            {
                Assert.Equal("/group/bar", routeEndpoint.RoutePattern.RawText);
                Assert.True(routeEndpoint.Metadata.Count >= 1);
                Assert.Equal("/group", routeEndpoint.Metadata.GetMetadata<string>());
            });
    }

    [Fact]
    public void DataSourceFiresChangeToken_WhenInnerDataSourceFiresChangeToken()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var dynamicDataSource = new DynamicEndpointDataSource();

        var group = builder.MapGroup("/group");
        ((IEndpointRouteBuilder)group).DataSources.Add(dynamicDataSource);

        var groupDataSource = GetEndpointDataSource(builder);

        var groupChangeToken = groupDataSource.GetChangeToken();
        Assert.False(groupChangeToken.HasChanged);

        dynamicDataSource.AddEndpoint(new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/foo"),
            0, null, null));

        Assert.True(groupChangeToken.HasChanged);

        var prefixedEndpoint = Assert.IsType<RouteEndpoint>(Assert.Single(groupDataSource.Endpoints));
        Assert.Equal("/group/foo", prefixedEndpoint.RoutePattern.RawText);
    }

    private sealed class TestCustomEndpoint : Endpoint
    {
        public TestCustomEndpoint() : base(null, null, null) { }
    }

    private sealed class TestCustomEndpintDataSource : EndpointDataSource
    {
        public override IReadOnlyList<Endpoint> Endpoints => new[] { new TestCustomEndpoint() };
        public override IChangeToken GetChangeToken() => throw new NotImplementedException();
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
