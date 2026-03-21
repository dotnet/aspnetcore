// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Tests;

public class ComponentEndpointRouteBuilderExtensionsTest
{
    [Fact]
    public void MapBlazorHub_WiresUp_UnderlyingHub()
    {
        // Arrange
        var applicationBuilder = CreateAppBuilder();
        var called = false;

        // Act
        var app = applicationBuilder
            .UseRouting()
            .UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub(dispatchOptions => called = true);
        }).Build();

        // Assert
        Assert.True(called);
    }

    [Fact]
    public void MapBlazorHub_MostGeneralOverload_MapsUnderlyingHub()
    {
        // Arrange
        var applicationBuilder = CreateAppBuilder();
        var called = false;

        // Act
        var app = applicationBuilder
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub("_blazor", dispatchOptions => called = true);
            }).Build();

        // Assert
        Assert.True(called);
    }

    [Fact]
    public void MapBlazorHub_AppliesFinalConventionToEachBuilder()
    {
        // Arrange
        var applicationBuilder = CreateAppBuilder();
        var buildersAffected = new List<string>();
        var called = false;

        // Act
        var app = applicationBuilder
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints
                .MapBlazorHub(dispatchOptions => called = true)
                .WithMetadata("initial-md")
                .Finally(builder =>
                {
                    if (builder.Metadata.Any(md => md is string smd && smd == "initial-md"))
                    {
                        buildersAffected.Add(builder.DisplayName);
                    }
                });
            }).Build();

        // Trigger endpoint construction
        app.Invoke(new DefaultHttpContext());

        // Assert
        Assert.True(called);
        // Final conventions are applied to each of the builders
        // in the Blazor component hub
        Assert.Equal(4, buildersAffected.Count);
        Assert.Contains("/_blazor/negotiate", buildersAffected);
        Assert.Contains("/_blazor", buildersAffected);
        Assert.Contains("Blazor disconnect", buildersAffected);
        Assert.Contains("Blazor initializers", buildersAffected);
    }

    [Fact]
    public void MapBlazorHub_AppliesFinalConventionsInFIFOOrder()
    {
        // Arrange
        var applicationBuilder = CreateAppBuilder();
        var called = false;
        var populatedMetadata = Array.Empty<string>();

        // Act
        var app = applicationBuilder
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                var builder = endpoints.MapBlazorHub(dispatchOptions => called = true);
                builder.Finally(b => b.Metadata.Add("first-in"));
                builder.Finally(b => b.Metadata.Add("last-in"));
                builder.Finally(b =>
                {
                    populatedMetadata = b.Metadata.OfType<string>().ToArray();
                });
            }).Build();

        // Trigger endpoint construction
        app.Invoke(new DefaultHttpContext());

        // Assert
        Assert.True(called);
        Assert.Equal(new[] { "first-in", "last-in" }, populatedMetadata);
    }

    private IApplicationBuilder CreateAppBuilder()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.ApplicationName).Returns("app");
        environment.SetupGet(e => e.WebRootFileProvider).Returns(new NullFileProvider());
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddLogging();
        services.AddOptions();
        var listener = new DiagnosticListener("Microsoft.AspNetCore");
        services.AddSingleton(listener);
        services.AddSingleton<DiagnosticSource>(listener);
        services.AddRouting();
        services.AddSignalR();
        services.AddServerSideBlazor();
        services.AddSingleton(environment.Object);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        var serviceProvider = services.BuildServiceProvider();

        return new ApplicationBuilder(serviceProvider);
    }

    private class MyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
            throw new System.NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new System.NotImplementedException();
        }
    }
}
