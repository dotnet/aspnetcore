// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.Builder.Extensions;

public class UsePathBaseExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    public void EmptyOrNullPathBase_DoNotAddMiddleware(string? pathBase)
    {
        // Arrange
        var useCalled = false;
        var builder = new ApplicationBuilderWrapper(CreateBuilder(), () => useCalled = true)
            .UsePathBase(pathBase);

        // Act
        builder.Build();

        // Assert
        Assert.False(useCalled);
    }

    private class ApplicationBuilderWrapper : IApplicationBuilder
    {
        private readonly IApplicationBuilder _wrappedBuilder;
        private readonly Action _useCallback;

        public ApplicationBuilderWrapper(IApplicationBuilder applicationBuilder, Action useCallback)
        {
            _wrappedBuilder = applicationBuilder;
            _useCallback = useCallback;
        }

        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            _useCallback();
            return _wrappedBuilder.Use(middleware);
        }

        public IServiceProvider ApplicationServices
        {
            get { return _wrappedBuilder.ApplicationServices; }
            set { _wrappedBuilder.ApplicationServices = value; }
        }

        public IDictionary<string, object?> Properties => _wrappedBuilder.Properties;
        public IFeatureCollection ServerFeatures => _wrappedBuilder.ServerFeatures;
        public RequestDelegate Build() => _wrappedBuilder.Build();
        public IApplicationBuilder New() => _wrappedBuilder.New();
    }

    [Theory]
    [InlineData("/base", "", "/base", "/base", "")]
    [InlineData("/base", "", "/base/", "/base", "/")]
    [InlineData("/base", "", "/base/something", "/base", "/something")]
    [InlineData("/base", "", "/base/something/", "/base", "/something/")]
    [InlineData("/base/more", "", "/base/more", "/base/more", "")]
    [InlineData("/base/more", "", "/base/more/something", "/base/more", "/something")]
    [InlineData("/base/more", "", "/base/more/something/", "/base/more", "/something/")]
    [InlineData("/base", "/oldbase", "/base", "/oldbase/base", "")]
    [InlineData("/base", "/oldbase", "/base/", "/oldbase/base", "/")]
    [InlineData("/base", "/oldbase", "/base/something", "/oldbase/base", "/something")]
    [InlineData("/base", "/oldbase", "/base/something/", "/oldbase/base", "/something/")]
    [InlineData("/base/more", "/oldbase", "/base/more", "/oldbase/base/more", "")]
    [InlineData("/base/more", "/oldbase", "/base/more/something", "/oldbase/base/more", "/something")]
    [InlineData("/base/more", "/oldbase", "/base/more/something/", "/oldbase/base/more", "/something/")]
    public Task RequestPathBaseContainingPathBase_IsSplit(string registeredPathBase, string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        return TestPathBase(registeredPathBase, pathBase, requestPath, expectedPathBase, expectedPath);
    }

    [Theory]
    [InlineData("/base", "", "/something", "", "/something")]
    [InlineData("/base", "", "/baseandsomething", "", "/baseandsomething")]
    [InlineData("/base", "", "/ba", "", "/ba")]
    [InlineData("/base", "", "/ba/se", "", "/ba/se")]
    [InlineData("/base", "/oldbase", "/something", "/oldbase", "/something")]
    [InlineData("/base", "/oldbase", "/baseandsomething", "/oldbase", "/baseandsomething")]
    [InlineData("/base", "/oldbase", "/ba", "/oldbase", "/ba")]
    [InlineData("/base", "/oldbase", "/ba/se", "/oldbase", "/ba/se")]
    public Task RequestPathBaseNotContainingPathBase_IsNotSplit(string registeredPathBase, string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        return TestPathBase(registeredPathBase, pathBase, requestPath, expectedPathBase, expectedPath);
    }

    [Theory]
    [InlineData("", "", "/", "", "/")]
    [InlineData("/", "", "/", "", "/")]
    [InlineData("/base", "", "/base/", "/base", "/")]
    [InlineData("/base/", "", "/base", "/base", "")]
    [InlineData("/base/", "", "/base/", "/base", "/")]
    [InlineData("", "/oldbase", "/", "/oldbase", "/")]
    [InlineData("/", "/oldbase", "/", "/oldbase", "/")]
    [InlineData("/base", "/oldbase", "/base/", "/oldbase/base", "/")]
    [InlineData("/base/", "/oldbase", "/base", "/oldbase/base", "")]
    [InlineData("/base/", "/oldbase", "/base/", "/oldbase/base", "/")]
    public Task PathBaseNeverEndsWithSlash(string registeredPathBase, string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        return TestPathBase(registeredPathBase, pathBase, requestPath, expectedPathBase, expectedPath);
    }

    [Theory]
    [InlineData("/base", "", "/Base/Something", "/Base", "/Something")]
    [InlineData("/base", "/OldBase", "/Base/Something", "/OldBase/Base", "/Something")]
    public Task PathBaseAndPathPreserveRequestCasing(string registeredPathBase, string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        return TestPathBase(registeredPathBase, pathBase, requestPath, expectedPathBase, expectedPath);
    }

    [Theory]
    [InlineData("/b♫se", "", "/b♫se/something", "/b♫se", "/something")]
    [InlineData("/b♫se", "", "/B♫se/something", "/B♫se", "/something")]
    [InlineData("/b♫se", "", "/b♫se/Something", "/b♫se", "/Something")]
    [InlineData("/b♫se", "/oldb♫se", "/b♫se/something", "/oldb♫se/b♫se", "/something")]
    [InlineData("/b♫se", "/oldb♫se", "/b♫se/Something", "/oldb♫se/b♫se", "/Something")]
    [InlineData("/b♫se", "/oldb♫se", "/B♫se/something", "/oldb♫se/B♫se", "/something")]
    public Task PathBaseCanHaveUnicodeCharacters(string registeredPathBase, string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        return TestPathBase(registeredPathBase, pathBase, requestPath, expectedPathBase, expectedPath);
    }

    [Theory]
    [InlineData("/b%42", "", "/b%42/something%42", "/b%42", "/something%42")]
    [InlineData("/b%42", "", "/B%42/something%42", "/B%42", "/something%42")]
    [InlineData("/b%42", "", "/b%42/Something%42", "/b%42", "/Something%42")]
    [InlineData("/b%42", "/oldb%42", "/b%42/something%42", "/oldb%42/b%42", "/something%42")]
    [InlineData("/b%42", "/oldb%42", "/b%42/Something%42", "/oldb%42/b%42", "/Something%42")]
    [InlineData("/b%42", "/oldb%42", "/B%42/something%42", "/oldb%42/B%42", "/something%42")]
    public Task PathBaseCanHavePercentCharacters(string registeredPathBase, string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        return TestPathBase(registeredPathBase, pathBase, requestPath, expectedPathBase, expectedPath);
    }

    [Fact]
    public async Task PathBaseWorksAfterUseRoutingIfGlobalRouteBuilderUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.UseRouting();

        app.UsePathBase("/base");

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/path", context => context.Response.WriteAsync("Response"));
        });

        await app.StartAsync();

        using var server = app.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("/base/path");

        Assert.Equal("Response", response);
    }

    [Fact]
    public async Task PathBaseWorksBeforeUseRoutingIfGlobalRouteBuilderUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.UsePathBase("/base");

        app.UseRouting();

        app.MapGet("/path", context => context.Response.WriteAsync("Response"));

        await app.StartAsync();

        using var server = app.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("/base/path");

        Assert.Equal("Response", response);
    }

    [Fact]
    public async Task PathBaseWorksWithoutUseRoutingWithWebApplication()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.UsePathBase("/base");

        app.MapGet("/path", context => context.Response.WriteAsync("Response"));

        await app.StartAsync();

        using var server = app.GetTestServer();

        var response = await server.CreateClient().GetStringAsync("/base/path");

        Assert.Equal("Response", response);
    }

    private static async Task TestPathBase(string registeredPathBase, string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        HttpContext requestContext = CreateRequest(pathBase, requestPath);
        var builder = CreateBuilder()
            .UsePathBase(new PathString(registeredPathBase));
        builder.Run(context =>
        {
            context.Items["test.Path"] = context.Request.Path;
            context.Items["test.PathBase"] = context.Request.PathBase;
            return Task.FromResult(0);
        });
        await builder.Build().Invoke(requestContext);

        // Assert path and pathBase are split after middleware
        Assert.Equal(expectedPath, ((PathString?)requestContext.Items["test.Path"])!.Value.Value);
        Assert.Equal(expectedPathBase, ((PathString?)requestContext.Items["test.PathBase"])!.Value.Value);

        // Assert path and pathBase are reset after request
        Assert.Equal(pathBase, requestContext.Request.PathBase.Value);
        Assert.Equal(requestPath, requestContext.Request.Path.Value);
    }

    private static HttpContext CreateRequest(string pathBase, string requestPath)
    {
        HttpContext context = new DefaultHttpContext();
        context.Request.PathBase = new PathString(pathBase);
        context.Request.Path = new PathString(requestPath);
        return context;
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return new ApplicationBuilder(new DummyServiceProvider());
    }

    private class DummyServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void AddService(Type type, object value) => _services[type] = value;

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            if (_services.TryGetValue(serviceType, out var value))
            {
                return value;
            }

            return null;
        }
    }
}
