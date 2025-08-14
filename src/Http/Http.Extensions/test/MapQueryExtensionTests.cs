// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests.Extensions;

public class MapQueryExtensionTests
{
    [Fact]
    public void MapQuery_WithRequestDelegate_ReturnsCorrectConventionBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRouting();
        var serviceProvider = services.BuildServiceProvider();

        var app = new ApplicationBuilder(serviceProvider);
        var endpoints = app.New().UseRouting().UseEndpoints(builder => { }).ApplicationServices.GetRequiredService<IEndpointRouteBuilder>();

        RequestDelegate handler = context => Task.CompletedTask;

        // Act
        var result = endpoints.MapQuery("/test", handler);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void MapQuery_WithDelegate_ReturnsCorrectRouteHandlerBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRouting();
        var serviceProvider = services.BuildServiceProvider();

        var app = new ApplicationBuilder(serviceProvider);
        var endpoints = app.New().UseRouting().UseEndpoints(builder => { }).ApplicationServices.GetRequiredService<IEndpointRouteBuilder>();

        Func<string> handler = () => "Hello";

        // Act
        var result = endpoints.MapQuery("/test", handler);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RouteHandlerBuilder>(result);
    }
}