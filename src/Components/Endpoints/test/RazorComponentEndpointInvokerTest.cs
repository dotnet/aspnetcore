// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests;
public class RazorComponentEndpointInvokerTest
{
    [Fact]
    public async Task Invoker_RejectsPostRequestsWithNonFormDataContentTypesAsync()
    {
        // Arrange
        var services = new ServiceCollection().AddRazorComponents()
                        .Services.AddAntiforgery()
                        .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                        .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
                        .BuildServiceProvider();

        var invoker = new RazorComponentEndpointInvoker(
            new EndpointHtmlRenderer(
                services,
                NullLoggerFactory.Instance),
            NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = new DefaultHttpContext();
        context.SetEndpoint(new RouteEndpoint(
            ctx => Task.CompletedTask,
            RoutePatternFactory.Parse("/"),
            0,
            new EndpointMetadataCollection(
                // These don't matter
                new ComponentTypeMetadata(typeof(AuthorizeView)),
                new RootComponentMetadata(typeof(AuthorizeView))),
            "test"));
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.RequestServices = services;

        // Act
        await invoker.Render(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider WebRootFileProvider { get => new NullFileProvider(); set => throw new NotImplementedException(); }
        public string EnvironmentName { get => "Development"; set => throw new NotImplementedException(); }
        public string ApplicationName { get => "Test"; set => throw new NotImplementedException(); }
        public string ContentRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
