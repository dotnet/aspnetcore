// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder.Extensions;

public class MapPathMiddlewareTests
{
    private static Task Success(HttpContext context)
    {
        context.Response.StatusCode = 200;
        context.Items["test.PathBase"] = context.Request.PathBase.Value;
        context.Items["test.Path"] = context.Request.Path.Value;
        return Task.FromResult<object?>(null);
    }

    private static void UseSuccess(IApplicationBuilder app)
    {
        app.Run(Success);
    }

    private static Task NotImplemented(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static void UseNotImplemented(IApplicationBuilder app)
    {
        app.Run(NotImplemented);
    }

    [Fact]
    public void NullArguments_ArgumentNullException()
    {
        var builder = new ApplicationBuilder(serviceProvider: null!);
        var noMiddleware = new ApplicationBuilder(serviceProvider: null!).Build();
        var noOptions = new MapOptions();
        Assert.Throws<ArgumentNullException>(() => builder.Map("/foo", configuration: null!));
        Assert.Throws<ArgumentNullException>(() => new MapMiddleware(noMiddleware, null!));
    }

    [Theory]
    [InlineData("/foo", "", "/foo", "/foo", "")]
    [InlineData("/foo", "", "/foo/", "/foo", "/")]
    [InlineData("/foo", "/Bar", "/foo", "/Bar/foo", "")]
    [InlineData("/foo", "/Bar", "/foo/cho", "/Bar/foo", "/cho")]
    [InlineData("/foo", "/Bar", "/foo/cho/", "/Bar/foo", "/cho/")]
    [InlineData("/foo/cho", "/Bar", "/foo/cho", "/Bar/foo/cho", "")]
    [InlineData("/foo/cho", "/Bar", "/foo/cho/do", "/Bar/foo/cho", "/do")]
    [InlineData("/foo%42/cho", "/Bar%42", "/foo%42/cho/do%42", "/Bar%42/foo%42/cho", "/do%42")]
    public async Task PathMatchFunc_BranchTaken(string matchPath, string basePath, string requestPath, string expectedPathBase, string expectedPath)
    {
        HttpContext context = CreateRequest(basePath, requestPath);
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.Map(new PathString(matchPath), UseSuccess);
        var app = builder.Build();
        await app.Invoke(context);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(basePath, context.Request.PathBase.Value);
        Assert.Equal(requestPath, context.Request.Path.Value);
        Assert.Equal(expectedPathBase, (string)context.Items["test.PathBase"]!);
        Assert.Equal(expectedPath, context.Items["test.Path"]);
    }

    [Theory]
    [InlineData("/foo", "", "/foo")]
    [InlineData("/foo", "", "/foo/")]
    [InlineData("/foo", "/Bar", "/foo")]
    [InlineData("/foo", "/Bar", "/foo/cho")]
    [InlineData("/foo", "/Bar", "/foo/cho/")]
    [InlineData("/foo/cho", "/Bar", "/foo/cho")]
    [InlineData("/foo/cho", "/Bar", "/foo/cho/do")]
    [InlineData("/foo", "", "/Foo")]
    [InlineData("/foo", "", "/Foo/")]
    [InlineData("/foo", "/Bar", "/Foo")]
    [InlineData("/foo", "/Bar", "/Foo/Cho")]
    [InlineData("/foo", "/Bar", "/Foo/Cho/")]
    [InlineData("/foo/cho", "/Bar", "/Foo/Cho")]
    [InlineData("/foo/cho", "/Bar", "/Foo/Cho/do")]
    public async Task PathMatchAction_BranchTaken(string matchPath, string basePath, string requestPath)
    {
        HttpContext context = CreateRequest(basePath, requestPath);
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.Map(matchPath, subBuilder => subBuilder.Run(Success));
        var app = builder.Build();
        await app.Invoke(context);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(string.Concat(basePath, requestPath.AsSpan(0, matchPath.Length)), (string)context.Items["test.PathBase"]!);
        Assert.Equal(requestPath.Substring(matchPath.Length), context.Items["test.Path"]);
    }

    [Theory]
    [InlineData("/foo", "", "/foo")]
    [InlineData("/foo", "", "/foo/")]
    [InlineData("/foo", "/Bar", "/foo")]
    [InlineData("/foo", "/Bar", "/foo/cho")]
    [InlineData("/foo", "/Bar", "/foo/cho/")]
    [InlineData("/foo/cho", "/Bar", "/foo/cho")]
    [InlineData("/foo/cho", "/Bar", "/foo/cho/do")]
    [InlineData("/foo", "", "/Foo")]
    [InlineData("/foo", "", "/Foo/")]
    [InlineData("/foo", "/Bar", "/Foo")]
    [InlineData("/foo", "/Bar", "/Foo/Cho")]
    [InlineData("/foo", "/Bar", "/Foo/Cho/")]
    [InlineData("/foo/cho", "/Bar", "/Foo/Cho")]
    [InlineData("/foo/cho", "/Bar", "/Foo/Cho/do")]
    public async Task PathMatchAction_BranchTaken_WithPreserveMatchedPathSegment(string matchPath, string basePath, string requestPath)
    {
        HttpContext context = CreateRequest(basePath, requestPath);
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.Map(matchPath, true, subBuilder => subBuilder.Run(Success));
        var app = builder.Build();
        await app.Invoke(context);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(basePath, (string)context.Items["test.PathBase"]!);
        Assert.Equal(requestPath, context.Items["test.Path"]);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/foo/")]
    [InlineData("/foo/cho/")]
    public void MatchPathWithTrailingSlashThrowsException(string matchPath)
    {
        Assert.Throws<ArgumentException>(() => new ApplicationBuilder(serviceProvider: null!).Map(matchPath, map => { }).Build());
    }

    [Theory]
    [InlineData("/foo", "", "")]
    [InlineData("/foo", "/bar", "")]
    [InlineData("/foo", "", "/bar")]
    [InlineData("/foo", "/foo", "")]
    [InlineData("/foo", "/foo", "/bar")]
    [InlineData("/foo", "", "/bar/foo")]
    [InlineData("/foo/bar", "/foo", "/bar")]
    public async Task PathMismatchFunc_PassedThrough(string matchPath, string basePath, string requestPath)
    {
        HttpContext context = CreateRequest(basePath, requestPath);
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.Map(matchPath, UseNotImplemented);
        builder.Run(Success);
        var app = builder.Build();
        await app.Invoke(context);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(basePath, context.Request.PathBase.Value);
        Assert.Equal(requestPath, context.Request.Path.Value);
    }

    [Theory]
    [InlineData("/foo", "", "")]
    [InlineData("/foo", "/bar", "")]
    [InlineData("/foo", "", "/bar")]
    [InlineData("/foo", "/foo", "")]
    [InlineData("/foo", "/foo", "/bar")]
    [InlineData("/foo", "", "/bar/foo")]
    [InlineData("/foo/bar", "/foo", "/bar")]
    public async Task PathMismatchAction_PassedThrough(string matchPath, string basePath, string requestPath)
    {
        HttpContext context = CreateRequest(basePath, requestPath);
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.Map(matchPath, UseNotImplemented);
        builder.Run(Success);
        var app = builder.Build();
        await app.Invoke(context);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(basePath, context.Request.PathBase.Value);
        Assert.Equal(requestPath, context.Request.Path.Value);
    }

    [Fact]
    public async Task ChainedRoutes_Success()
    {
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.Map("/route1", map =>
        {
            map.Map("/subroute1", UseSuccess);
            map.Run(NotImplemented);
        });
        builder.Map("/route2/subroute2", UseSuccess);
        var app = builder.Build();

        HttpContext context = CreateRequest(string.Empty, "/route1");
        await Assert.ThrowsAsync<NotImplementedException>(() => app.Invoke(context));

        context = CreateRequest(string.Empty, "/route1/subroute1");
        await app.Invoke(context);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(string.Empty, context.Request.PathBase.Value);
        Assert.Equal("/route1/subroute1", context.Request.Path.Value);

        context = CreateRequest(string.Empty, "/route2");
        await app.Invoke(context);
        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal(string.Empty, context.Request.PathBase.Value);
        Assert.Equal("/route2", context.Request.Path.Value);

        context = CreateRequest(string.Empty, "/route2/subroute2");
        await app.Invoke(context);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(string.Empty, context.Request.PathBase.Value);
        Assert.Equal("/route2/subroute2", context.Request.Path.Value);

        context = CreateRequest(string.Empty, "/route2/subroute2/subsub2");
        await app.Invoke(context);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(string.Empty, context.Request.PathBase.Value);
        Assert.Equal("/route2/subroute2/subsub2", context.Request.Path.Value);
    }

    [Fact]
    public void ApplicationBuilderMapOverloadPreferredOverEndpointBuilderGivenStringPathAndImplicitLambdaParameterType()
    {
        var mockWebApplication = new MockWebApplication();

        mockWebApplication.Map("/foo", app => { });

        Assert.True(mockWebApplication.UseCalled);
    }

    [Fact]
    public void ApplicationBuilderMapOverloadPreferredOverEndpointBuilderGivenStringPathAndExplicitLambdaParameterType()
    {
        var mockWebApplication = new MockWebApplication();

        mockWebApplication.Map("/foo", (IApplicationBuilder app) => { });

        Assert.True(mockWebApplication.UseCalled);
    }

    private HttpContext CreateRequest(string basePath, string requestPath)
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.PathBase = new PathString(basePath);
        context.Request.Path = new PathString(requestPath);
        return context;
    }

    private class MockWebApplication : IApplicationBuilder, IEndpointRouteBuilder
    {
        public bool UseCalled { get; set; }

        public IServiceProvider ApplicationServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IFeatureCollection ServerFeatures => throw new NotImplementedException();

        public IDictionary<string, object?> Properties => throw new NotImplementedException();

        public IServiceProvider ServiceProvider => throw new NotImplementedException();

        public ICollection<EndpointDataSource> DataSources => throw new NotImplementedException();

        public IApplicationBuilder CreateApplicationBuilder() => throw new NotImplementedException();

        public IApplicationBuilder New() => this;

        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            UseCalled = true;
            return this;
        }

        public RequestDelegate Build()
        {
            return context => Task.CompletedTask;
        }
    }
}
