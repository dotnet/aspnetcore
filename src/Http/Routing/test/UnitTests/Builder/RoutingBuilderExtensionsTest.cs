// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Builder;

public class RoutingBuilderExtensionsTest
{
    [Fact]
    public void UseRouter_ThrowsInvalidOperationException_IfRoutingMarkerServiceIsNotRegistered()
    {
        // Arrange
        var applicationBuilderMock = new Mock<IApplicationBuilder>();
        applicationBuilderMock
            .Setup(s => s.ApplicationServices)
            .Returns(Mock.Of<IServiceProvider>());

        var router = Mock.Of<IRouter>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => applicationBuilderMock.Object.UseRouter(router));

        Assert.Equal(
            "Unable to find the required services. Please add all the required services by calling " +
            "'IServiceCollection.AddRouting' inside the call to 'ConfigureServices(...)'" +
            " in the application startup code.",
            exception.Message);
    }

    [Fact]
    public void UseRouter_IRouter_ThrowsWithoutCallingAddRouting()
    {
        // Arrange
        var app = new ApplicationBuilder(Mock.Of<IServiceProvider>());

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => app.UseRouter(Mock.Of<IRouter>()));

        // Assert
        Assert.Equal(
            "Unable to find the required services. " +
            "Please add all the required services by calling 'IServiceCollection.AddRouting' " +
            "inside the call to 'ConfigureServices(...)' in the application startup code.",
            ex.Message);
    }

    [Fact]
    public void UseRouter_Action_ThrowsWithoutCallingAddRouting()
    {
        // Arrange
        var app = new ApplicationBuilder(Mock.Of<IServiceProvider>());

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => app.UseRouter(b => { }));

        // Assert
        Assert.Equal(
            "Unable to find the required services. " +
            "Please add all the required services by calling 'IServiceCollection.AddRouting' " +
            "inside the call to 'ConfigureServices(...)' in the application startup code.",
            ex.Message);
    }

    [Fact]
    public async Task UseRouter_IRouter_CallsRoute()
    {
        // Arrange
        var services = CreateServices();

        var app = new ApplicationBuilder(services);

        var router = new Mock<IRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        app.UseRouter(router.Object);

        var appFunc = app.Build();

        // Act
        await appFunc(new DefaultHttpContext());

        // Assert
        router.Verify();
    }

    [Fact]
    public async Task UseRouter_Action_CallsRoute()
    {
        // Arrange
        var services = CreateServices();

        var app = new ApplicationBuilder(services);

        var router = new Mock<IRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        app.UseRouter(b =>
        {
            b.Routes.Add(router.Object);
        });

        var appFunc = app.Build();

        // Act
        await appFunc(new DefaultHttpContext());

        // Assert
        router.Verify();
    }

    private IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();
        services.AddRouting();

        return services.BuildServiceProvider();
    }
}
