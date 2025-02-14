// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Builder;

public class EndpointRoutingApplicationBuilderExtensionsTest
{
    [Fact]
    public void UseRouting_ServicesNotRegistered_Throws()
    {
        // Arrange
        var app = new ApplicationBuilder(Mock.Of<IServiceProvider>());

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => app.UseRouting());

        // Assert
        Assert.Equal(
            "Unable to find the required services. " +
            "Please add all the required services by calling 'IServiceCollection.AddRouting' " +
            "inside the call to 'ConfigureServices(...)' in the application startup code.",
            ex.Message);
    }

    [Fact]
    public void UseEndpoint_ServicesNotRegistered_Throws()
    {
        // Arrange
        var app = new ApplicationBuilder(Mock.Of<IServiceProvider>());

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => app.UseEndpoints(endpoints => { }));

        // Assert
        Assert.Equal(
            "Unable to find the required services. " +
            "Please add all the required services by calling 'IServiceCollection.AddRouting' " +
            "inside the call to 'ConfigureServices(...)' in the application startup code.",
            ex.Message);
    }

    [Fact]
    public async Task UseRouting_ServicesRegistered_NoMatch_DoesNotSetFeature()
    {
        // Arrange
        var services = CreateServices();

        var app = new ApplicationBuilder(services);

        app.UseRouting();

        var appFunc = app.Build();
        var httpContext = new DefaultHttpContext();

        // Act
        await appFunc(httpContext);

        // Assert
        Assert.Null(httpContext.Features.Get<IEndpointFeature>());
    }

    [Fact]
    public async Task UseRouting_ServicesRegistered_Match_DoesNotSetsFeature()
    {
        // Arrange
        var endpoint = new RouteEndpoint(
           TestConstants.EmptyRequestDelegate,
           RoutePatternFactory.Parse("{*p}"),
           0,
           EndpointMetadataCollection.Empty,
           "Test");

        var services = CreateServices();

        var app = new ApplicationBuilder(services);

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.DataSources.Add(new DefaultEndpointDataSource(endpoint));
        });

        var appFunc = app.Build();
        var httpContext = new DefaultHttpContext { RequestServices = services };

        // Act
        await appFunc(httpContext);

        // Assert
        var feature = httpContext.Features.Get<IEndpointFeature>();
        Assert.NotNull(feature);
        Assert.Same(endpoint, httpContext.GetEndpoint());
    }

    [Fact]
    public void UseEndpoint_WithoutEndpointRoutingMiddleware_Throws()
    {
        // Arrange
        var services = CreateServices();

        var app = new ApplicationBuilder(services);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => app.UseEndpoints(endpoints => { }));

        // Assert
        Assert.Equal(
            "EndpointRoutingMiddleware matches endpoints setup by EndpointMiddleware and so must be added to the request " +
            "execution pipeline before EndpointMiddleware. " +
            "Please add EndpointRoutingMiddleware by calling 'IApplicationBuilder.UseRouting' " +
            "inside the call to 'Configure(...)' in the application startup code.",
            ex.Message);
    }

    [Fact]
    public void UseEndpoint_WithApplicationBuilderMismatch_Throws()
    {
        // Arrange
        var services = CreateServices();

        var app = new ApplicationBuilder(services);

        app.UseRouting();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => app.Map("/Test", b => b.UseEndpoints(endpoints => { })));

        // Assert
        Assert.Equal(
            "The EndpointRoutingMiddleware and EndpointMiddleware must be added to the same IApplicationBuilder instance. " +
            "To use Endpoint Routing with 'Map(...)', make sure to call 'IApplicationBuilder.UseRouting' before " +
            "'IApplicationBuilder.UseEndpoints' for each branch of the middleware pipeline.",
            ex.Message);
    }

    [Fact]
    public async Task UseEndpoint_ServicesRegisteredAndEndpointRoutingRegistered_NoMatch_DoesNotSetFeature()
    {
        // Arrange
        var services = CreateServices();

        var app = new ApplicationBuilder(services);

        app.UseRouting();
        app.UseEndpoints(endpoints => { });

        var appFunc = app.Build();
        var httpContext = new DefaultHttpContext();

        // Act
        await appFunc(httpContext);

        // Assert
        Assert.Null(httpContext.Features.Get<IEndpointFeature>());
    }

    [Fact]
    public void UseEndpoints_CallWithBuilder_SetsEndpointDataSource()
    {
        // Arrange
        var matcherEndpointDataSources = new List<EndpointDataSource>();
        var matcherFactoryMock = new Mock<MatcherFactory>();
        matcherFactoryMock
            .Setup(m => m.CreateMatcher(It.IsAny<EndpointDataSource>()))
            .Callback((EndpointDataSource arg) =>
            {
                matcherEndpointDataSources.Add(arg);
            })
            .Returns(new TestMatcher(false));

        var services = CreateServices(matcherFactoryMock.Object);

        var app = new ApplicationBuilder(services);

        // Act
        app.UseRouting();
        app.UseEndpoints(builder =>
        {
            builder.Map("/1", d => null).WithDisplayName("Test endpoint 1");
            builder.Map("/2", d => null).WithDisplayName("Test endpoint 2");
        });

        app.UseRouting();
        app.UseEndpoints(builder =>
        {
            builder.Map("/3", d => null).WithDisplayName("Test endpoint 3");
            builder.Map("/4", d => null).WithDisplayName("Test endpoint 4");
        });

        // This triggers the middleware to be created and the matcher factory to be called
        // with the datasource we want to test
        var requestDelegate = app.Build();
        requestDelegate(new DefaultHttpContext());

        // Assert
        Assert.Equal(2, matcherEndpointDataSources.Count);

        // each UseRouter has its own data source collection
        Assert.Collection(matcherEndpointDataSources[0].Endpoints,
            e => Assert.Equal("Test endpoint 1", e.DisplayName),
            e => Assert.Equal("Test endpoint 2", e.DisplayName));

        Assert.Collection(matcherEndpointDataSources[1].Endpoints,
            e => Assert.Equal("Test endpoint 3", e.DisplayName),
            e => Assert.Equal("Test endpoint 4", e.DisplayName));

        var compositeEndpointBuilder = services.GetRequiredService<EndpointDataSource>();

        // Global collection has all endpoints
        Assert.Collection(compositeEndpointBuilder.Endpoints,
            e => Assert.Equal("Test endpoint 1", e.DisplayName),
            e => Assert.Equal("Test endpoint 2", e.DisplayName),
            e => Assert.Equal("Test endpoint 3", e.DisplayName),
            e => Assert.Equal("Test endpoint 4", e.DisplayName));
    }

    // Verifies that it's possible to use endpoints and map together.
    [Fact]
    public void UseEndpoints_CallWithBuilder_SetsEndpointDataSource_WithMap()
    {
        // Arrange
        var matcherEndpointDataSources = new List<EndpointDataSource>();
        var matcherFactoryMock = new Mock<MatcherFactory>();
        matcherFactoryMock
            .Setup(m => m.CreateMatcher(It.IsAny<EndpointDataSource>()))
            .Callback((EndpointDataSource arg) =>
            {
                matcherEndpointDataSources.Add(arg);
            })
            .Returns(new TestMatcher(false));

        var services = CreateServices(matcherFactoryMock.Object);

        var app = new ApplicationBuilder(services);

        // Act
        app.UseRouting();

        app.Map("/foo", b =>
        {
            b.UseRouting();
            b.UseEndpoints(builder =>
            {
                builder.Map("/1", d => null).WithDisplayName("Test endpoint 1");
                builder.Map("/2", d => null).WithDisplayName("Test endpoint 2");
            });
        });

        app.UseEndpoints(builder =>
        {
            builder.Map("/3", d => null).WithDisplayName("Test endpoint 3");
            builder.Map("/4", d => null).WithDisplayName("Test endpoint 4");
        });

        // This triggers the middleware to be created and the matcher factory to be called
        // with the datasource we want to test
        var requestDelegate = app.Build();
        requestDelegate(new DefaultHttpContext());
        requestDelegate(new DefaultHttpContext() { Request = { Path = "/Foo", }, });

        // Assert
        Assert.Equal(2, matcherEndpointDataSources.Count);

        // Each UseRouter has its own data source
        Assert.Collection(matcherEndpointDataSources[1].Endpoints, // app.UseRouter
            e => Assert.Equal("Test endpoint 1", e.DisplayName),
            e => Assert.Equal("Test endpoint 2", e.DisplayName));

        Assert.Collection(matcherEndpointDataSources[0].Endpoints, // b.UseRouter
            e => Assert.Equal("Test endpoint 3", e.DisplayName),
            e => Assert.Equal("Test endpoint 4", e.DisplayName));

        var compositeEndpointBuilder = services.GetRequiredService<EndpointDataSource>();

        // Global middleware has all endpoints
        Assert.Collection(compositeEndpointBuilder.Endpoints,
            e => Assert.Equal("Test endpoint 1", e.DisplayName),
            e => Assert.Equal("Test endpoint 2", e.DisplayName),
            e => Assert.Equal("Test endpoint 3", e.DisplayName),
            e => Assert.Equal("Test endpoint 4", e.DisplayName));
    }

    [Fact]
    public void UseEndpoints_WithGlobalEndpointRouteBuilderHasRoutes()
    {
        // Arrange
        var services = CreateServices();

        var app = new ApplicationBuilder(services);

        var mockRouteBuilder = new Mock<IEndpointRouteBuilder>();
        mockRouteBuilder.Setup(m => m.DataSources).Returns(new List<EndpointDataSource>());

        var routeBuilder = mockRouteBuilder.Object;
        app.Properties.Add("__GlobalEndpointRouteBuilder", routeBuilder);
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/1", d => Task.CompletedTask).WithDisplayName("Test endpoint 1");
        });

        var requestDelegate = app.Build();

        var endpointDataSource = Assert.Single(mockRouteBuilder.Object.DataSources);
        Assert.Collection(endpointDataSource.Endpoints,
            e => Assert.Equal("Test endpoint 1", e.DisplayName));

        var routeOptions = app.ApplicationServices.GetRequiredService<IOptions<RouteOptions>>();
        Assert.Equal(mockRouteBuilder.Object.DataSources, routeOptions.Value.EndpointDataSources);
    }

    [Fact]
    public void UseRouting_SetsEndpointRouteBuilder_IfGlobalOneExists()
    {
        // Arrange
        var services = CreateServices();

        var app = new ApplicationBuilder(services);

        var routeBuilder = new Mock<IEndpointRouteBuilder>().Object;
        app.Properties.Add("__GlobalEndpointRouteBuilder", routeBuilder);
        app.UseRouting();

        Assert.True(app.Properties.TryGetValue("__EndpointRouteBuilder", out var local));
        Assert.True(app.Properties.TryGetValue("__GlobalEndpointRouteBuilder", out var global));
        Assert.Same(local, global);
    }

    private IServiceProvider CreateServices()
    {
        return CreateServices(matcherFactory: null);
    }

    private IServiceProvider CreateServices(MatcherFactory matcherFactory)
    {
        var services = new ServiceCollection();

        if (matcherFactory != null)
        {
            services.AddSingleton<MatcherFactory>(matcherFactory);
        }

        services.AddMetrics();
        services.AddLogging();
        services.AddOptions();
        services.AddRouting();
        var listener = new DiagnosticListener("Microsoft.AspNetCore");
        services.AddSingleton(listener);
        services.AddSingleton<DiagnosticSource>(listener);
        services.AddSingleton(Mock.Of<IHostEnvironment>());

        var serviceProvder = services.BuildServiceProvider();

        return serviceProvder;
    }
}
