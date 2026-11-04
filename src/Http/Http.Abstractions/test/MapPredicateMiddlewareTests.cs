// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.Extensions;

using Predicate = Func<HttpContext, bool>;

public class MapPredicateMiddlewareTests
{
    private static readonly Predicate NotImplementedPredicate = new Predicate(environment => { throw new NotImplementedException(); });

    private static Task Success(HttpContext context)
    {
        context.Response.StatusCode = 200;
        return Task.FromResult<object>(null!);
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

    private bool TruePredicate(HttpContext context)
    {
        return true;
    }

    private bool FalsePredicate(HttpContext context)
    {
        return false;
    }

    [Fact]
    public void NullArguments_ArgumentNullException()
    {
        var builder = new ApplicationBuilder(serviceProvider: null!);
        var noMiddleware = new ApplicationBuilder(serviceProvider: null!).Build();
        var noOptions = new MapWhenOptions();
        Assert.Throws<ArgumentNullException>(() => builder.MapWhen(null!, UseNotImplemented));
        Assert.Throws<ArgumentNullException>(() => builder.MapWhen(NotImplementedPredicate, configuration: null!));
        Assert.Throws<ArgumentNullException>(() => new MapWhenMiddleware(null!, noOptions));
        Assert.Throws<ArgumentNullException>(() => new MapWhenMiddleware(noMiddleware, null!));
        Assert.Throws<ArgumentNullException>(() => new MapWhenMiddleware(null!, noOptions));
        Assert.Throws<ArgumentNullException>(() => new MapWhenMiddleware(noMiddleware, null!));
    }

    [Fact]
    public async Task PredicateTrue_BranchTaken()
    {
        HttpContext context = CreateRequest();
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.MapWhen(TruePredicate, UseSuccess);
        var app = builder.Build();
        await app.Invoke(context);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task PredicateTrueAction_BranchTaken()
    {
        HttpContext context = CreateRequest();
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.MapWhen(TruePredicate, UseSuccess);
        var app = builder.Build();
        await app.Invoke(context);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task PredicateFalseAction_PassThrough()
    {
        HttpContext context = CreateRequest();
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.MapWhen(FalsePredicate, UseNotImplemented);
        builder.Run(Success);
        var app = builder.Build();
        await app.Invoke(context);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task ChainedPredicates_Success()
    {
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.MapWhen(TruePredicate, map1 =>
        {
            map1.MapWhen((Predicate)FalsePredicate, UseNotImplemented);
            map1.MapWhen((Predicate)TruePredicate, map2 => map2.MapWhen((Predicate)TruePredicate, UseSuccess));
            map1.Run(NotImplemented);
        });
        var app = builder.Build();

        HttpContext context = CreateRequest();
        await app.Invoke(context);
        Assert.Equal(200, context.Response.StatusCode);
    }

    private HttpContext CreateRequest()
    {
        HttpContext context = new DefaultHttpContext();
        return context;
    }
}
