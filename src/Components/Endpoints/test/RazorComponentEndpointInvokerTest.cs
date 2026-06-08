// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints.Tests.TestComponents;
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

    [Fact]
    public async Task Invoker_HandlesHeadRequestAsync()
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
                new ComponentTypeMetadata(typeof(SimpleComponent)),
                new RootComponentMetadata(typeof(SimpleComponent)),
                new ConfiguredRenderModesMetadata(Array.Empty<IComponentRenderMode>())),
            "test"));
        context.Request.Method = "HEAD";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Path = "/";
        context.Response.Body = new MemoryStream();
        context.RequestServices = services;

        // Act
        await invoker.Render(context);

        // Assert
        // HEAD requests should execute the full request like GET, returning 200 OK with headers.
        // The HTTP server (Kestrel) handles suppressing the response body for HEAD requests.
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", context.Response.ContentType);
    }

    [Fact]
    public async Task Invoker_PostReturns400_WhenAntiforgeryValidationFeatureIsInvalid()
    {
        var services = new ServiceCollection().AddRazorComponents()
                        .Services.AddAntiforgery()
                        .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                        .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
                        .BuildServiceProvider();

        var invoker = new RazorComponentEndpointInvoker(
            new EndpointHtmlRenderer(services, NullLoggerFactory.Instance),
            NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = BuildPostContext(services, "name=alice");
        context.Features.Set<IAntiforgeryValidationFeature>(new InvalidAntiforgeryValidationFeature());

        await invoker.Render(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("antiforgery token", await ReadBody(context), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invoker_PostDoesNotFailAntiforgery_WhenAntiforgeryValidationFeatureIsValid()
    {
        var services = new ServiceCollection().AddRazorComponents()
                        .Services.AddAntiforgery()
                        .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                        .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
                        .BuildServiceProvider();

        var invoker = new RazorComponentEndpointInvoker(
            new EndpointHtmlRenderer(services, NullLoggerFactory.Instance),
            NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = BuildPostContext(services, "name=alice");
        context.Features.Set<IAntiforgeryValidationFeature>(new ValidAntiforgeryValidationFeature());

        await invoker.Render(context);

        Assert.DoesNotContain("antiforgery token", await ReadBody(context), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invoker_PostDoesNotFailAntiforgery_WhenNoAntiforgeryValidationFeatureIsSet()
    {
        var services = new ServiceCollection().AddRazorComponents()
                        .Services.AddAntiforgery()
                        .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                        .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
                        .BuildServiceProvider();

        var invoker = new RazorComponentEndpointInvoker(
            new EndpointHtmlRenderer(services, NullLoggerFactory.Instance),
            NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = BuildPostContext(services, "name=alice");

        await invoker.Render(context);

        Assert.DoesNotContain("antiforgery token", await ReadBody(context), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private static DefaultHttpContext BuildPostContext(IServiceProvider services, string formBody)
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new RouteEndpoint(
            ctx => Task.CompletedTask,
            RoutePatternFactory.Parse("/"),
            0,
            new EndpointMetadataCollection(
                new ComponentTypeMetadata(typeof(SimpleComponent)),
                new RootComponentMetadata(typeof(SimpleComponent)),
                new ConfiguredRenderModesMetadata(Array.Empty<IComponentRenderMode>())),
            "test"));
        context.Request.Method = "POST";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Path = "/";
        context.Request.ContentType = "application/x-www-form-urlencoded";
        var body = Encoding.UTF8.GetBytes(formBody);
        context.Request.Body = new MemoryStream(body);
        context.Request.ContentLength = body.Length;
        context.Response.Body = new MemoryStream();
        context.RequestServices = services;
        return context;
    }

    private sealed class InvalidAntiforgeryValidationFeature : IAntiforgeryValidationFeature
    {
        public bool IsValid => false;
        public Exception Error { get; } = new AntiforgeryValidationException("invalid");
    }

    private sealed class ValidAntiforgeryValidationFeature : IAntiforgeryValidationFeature
    {
        public bool IsValid => true;
        public Exception Error => null;
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
