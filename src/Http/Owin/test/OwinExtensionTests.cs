// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Owin;

using AddMiddleware = Action<Func<
      Func<IDictionary<string, object>, Task>,
      Func<IDictionary<string, object>, Task>
    >>;
using AppFunc = Func<IDictionary<string, object>, Task>;
using CreateMiddleware = Func<
      Func<IDictionary<string, object>, Task>,
      Func<IDictionary<string, object>, Task>
    >;

public class OwinExtensionTests
{
    static readonly AppFunc notFound = env => new Task(() => { env["owin.ResponseStatusCode"] = 404; });

    [Fact]
    public async Task OwinConfigureServiceProviderAddsServices()
    {
        var list = new List<CreateMiddleware>();
        AddMiddleware build = list.Add;
        IServiceProvider serviceProvider = null;
        FakeService fakeService = null;

        var builder = build.UseBuilder(applicationBuilder =>
        {
            serviceProvider = applicationBuilder.ApplicationServices;
            applicationBuilder.Run(context =>
            {
                fakeService = context.RequestServices.GetService<FakeService>();
                return Task.FromResult(0);
            });
        },
        new ServiceCollection().AddSingleton(new FakeService()).BuildServiceProvider());

        list.Reverse();
        await list
            .Aggregate(notFound, (next, middleware) => middleware(next))
            .Invoke(new Dictionary<string, object>());

        Assert.NotNull(serviceProvider);
        Assert.NotNull(serviceProvider.GetService<FakeService>());
        Assert.NotNull(fakeService);
    }

    [Fact]
    public async Task OwinDefaultNoServices()
    {
        var list = new List<CreateMiddleware>();
        AddMiddleware build = list.Add;
        IServiceProvider expectedServiceProvider = new ServiceCollection().BuildServiceProvider();
        IServiceProvider serviceProvider = null;
        FakeService fakeService = null;
        bool builderExecuted = false;
        bool applicationExecuted = false;

        var builder = build.UseBuilder(applicationBuilder =>
        {
            builderExecuted = true;
            serviceProvider = applicationBuilder.ApplicationServices;
            applicationBuilder.Run(context =>
            {
                applicationExecuted = true;
                fakeService = context.RequestServices.GetService<FakeService>();
                return Task.FromResult(0);
            });
        },
        expectedServiceProvider);

        list.Reverse();
        await list
            .Aggregate(notFound, (next, middleware) => middleware(next))
            .Invoke(new Dictionary<string, object>());

        Assert.True(builderExecuted);
        Assert.Equal(expectedServiceProvider, serviceProvider);
        Assert.True(applicationExecuted);
        Assert.Null(fakeService);
    }

    [Fact]
    public async Task OwinDefaultNullServiceProvider()
    {
        var list = new List<CreateMiddleware>();
        AddMiddleware build = list.Add;
        IServiceProvider serviceProvider = null;
        FakeService fakeService = null;
        bool builderExecuted = false;
        bool applicationExecuted = false;

        var builder = build.UseBuilder(applicationBuilder =>
        {
            builderExecuted = true;
            serviceProvider = applicationBuilder.ApplicationServices;
            applicationBuilder.Run(context =>
            {
                applicationExecuted = true;
                fakeService = context.RequestServices.GetService<FakeService>();
                return Task.FromResult(0);
            });
        });

        list.Reverse();
        await list
            .Aggregate(notFound, (next, middleware) => middleware(next))
            .Invoke(new Dictionary<string, object>());

        Assert.True(builderExecuted);
        Assert.NotNull(serviceProvider);
        Assert.True(applicationExecuted);
        Assert.Null(fakeService);
    }

    [Fact]
    public async Task UseOwin()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var builder = new ApplicationBuilder(serviceProvider);
        IDictionary<string, object> environment = null;
        var context = new DefaultHttpContext();

        builder.UseOwin(addToPipeline =>
        {
            addToPipeline(next =>
            {
                Assert.NotNull(next);
                return async env =>
                {
                    environment = env;
                    await next(env);
                };
            });
        });
        await builder.Build().Invoke(context);

        // Dictionary contains context but does not contain "websocket.Accept" or "websocket.AcceptAlt" keys.
        Assert.NotNull(environment);
        var value = Assert.Single(
                environment,
                kvp => string.Equals(typeof(HttpContext).FullName, kvp.Key, StringComparison.Ordinal))
            .Value;
        Assert.Equal(context, value);
        Assert.False(environment.ContainsKey("websocket.Accept"));
        Assert.False(environment.ContainsKey("websocket.AcceptAlt"));
    }

    private class FakeService
    {
    }
}
