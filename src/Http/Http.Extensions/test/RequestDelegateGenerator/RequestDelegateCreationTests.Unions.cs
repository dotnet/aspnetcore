// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public union TestUnionIntString(int, string);

public abstract partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
{
    [Fact]
    public void JsonTypeInfo_For_UnionType_HasUnionKind_AndPopulatedCases()
    {
        var options = JsonSerializerOptions.Default;
        var info = options.GetTypeInfo(typeof(TestUnionIntString));

        Assert.Equal(JsonTypeInfoKind.Union, info.Kind);
        Assert.NotNull(info.UnionCases);
        Assert.Equal(2, info.UnionCases.Count);
        Assert.NotNull(info.UnionConstructor);
    }

    [Fact]
    public async Task MapAction_ReturnsUnion_BothCases_SerializeTransparently()
    {
        var source = """
            app.MapGet("/int",    () => new UnionIntString(42));
            app.MapGet("/string", () => new UnionIntString("hello union!"));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        // /int → 42
        var intEndpoint = endpoints.OfType<RouteEndpoint>().Single(e => e.RoutePattern.RawText == "/int");
        var ctx1 = CreateHttpContext();
        await intEndpoint.RequestDelegate(ctx1);
        await VerifyResponseBodyAsync(ctx1, "42");

        // /string → "hello union!"
        var stringEndpoint = endpoints.OfType<RouteEndpoint>().Single(e => e.RoutePattern.RawText == "/string");
        var ctx2 = CreateHttpContext();
        await stringEndpoint.RequestDelegate(ctx2);
        await VerifyResponseBodyAsync(ctx2, "\"hello union!\"");
    }

    [Fact]
    public async Task MapAction_TaskAndValueTaskUnion_SerializeActiveCase()
    {
        var source = """
            app.MapGet("/sync",      () => new UnionIntString(42));
            app.MapGet("/task",      () => Task.FromResult(new UnionIntString(42)));
            app.MapGet("/valuetask", () => ValueTask.FromResult(new UnionIntString(42)));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        foreach (var route in new[] { "/sync", "/task", "/valuetask" })
        {
            var endpoint = endpoints.Single(e => e.RoutePattern.RawText == route);
            var ctx = CreateHttpContext();
            await endpoint.RequestDelegate(ctx);
            await VerifyResponseBodyAsync(ctx, "42");
        }
    }

    [Fact]
    public async Task MapAction_ReturnsNullableUnionWrapper_HandlesValueAndNull()
    {
        var source = """
            app.MapGet("/value", () => (UnionIntString?)new UnionIntString(42));
            app.MapGet("/null",  () => (UnionIntString?)null);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var valueCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/value").RequestDelegate(valueCtx);
        await VerifyResponseBodyAsync(valueCtx, "42");

        var nullCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/null").RequestDelegate(nullCtx);
        await VerifyResponseBodyAsync(nullCtx, "null");
    }

    [Fact]
    public async Task MapAction_ReturnsUnionWithNullableCase_HandlesEachCase()
    {
        var source = """
            app.MapGet("/int",    () => new NullableCaseUnion((int?)5));
            app.MapGet("/null",   () => new NullableCaseUnion((int?)null));
            app.MapGet("/string", () => new NullableCaseUnion("hi"));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var intCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/int").RequestDelegate(intCtx);
        await VerifyResponseBodyAsync(intCtx, "5");

        var nullCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/null").RequestDelegate(nullCtx);
        await VerifyResponseBodyAsync(nullCtx, "null");

        var stringCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/string").RequestDelegate(stringCtx);
        await VerifyResponseBodyAsync(stringCtx, "\"hi\"");
    }

    [Fact]
    public async Task MapAction_ReturnsObjectCaseUnion_SerializesActiveCase()
    {
        var source = """
            app.MapGet("/cat", () => new Pet(new Cat("Whiskers")));
            app.MapGet("/dog", () => new Pet(new Dog("Labrador")));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var catCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/cat").RequestDelegate(catCtx);
        await VerifyResponseBodyAsync(catCtx, "{\"name\":\"Whiskers\"}");

        var dogCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/dog").RequestDelegate(dogCtx);
        await VerifyResponseBodyAsync(dogCtx, "{\"breed\":\"Labrador\"}");
    }
}
