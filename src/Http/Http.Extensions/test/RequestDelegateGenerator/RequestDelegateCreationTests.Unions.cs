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

    // Each primitive .NET type, paired with a sibling case of a different JSON
    // token kind so dispatch is unambiguous. Verifies STJ's per-converter wire
    // format flows through RDF/RDG correctly for every primitive.
    [Theory]
    [InlineData("/byte", "42", "Number")]
    [InlineData("/short", "1234", "Number")]
    [InlineData("/int", "42", "Number")]
    [InlineData("/long", "9999999999", "Number")]
    [InlineData("/decimal", "3.14", "Number")]
    [InlineData("/double", "2.5", "Number")]
    [InlineData("/bool", "true", "Boolean")]
    [InlineData("/guid", "\"00000000-0000-0000-0000-000000000001\"", "String")]
    [InlineData("/datetime", "\"2024-05-28T10:00:00\"", "String")]
    [InlineData("/char", "\"x\"", "String")]
    [InlineData("/string", "\"hi\"", "String")]
    public async Task MapAction_ReturnsUnion_PrimitiveCases_SerializeAsExpected(
        string route, string expectedBody, string tokenKind)
    {
        var source = """
            app.MapGet("/byte",     () => new UnionByteString((byte)42));
            app.MapGet("/short",    () => new UnionShortString((short)1234));
            app.MapGet("/int",      () => new UnionIntString(42));
            app.MapGet("/long",     () => new UnionLongString(9999999999L));
            app.MapGet("/decimal",  () => new UnionDecimalString(3.14m));
            app.MapGet("/double",   () => new UnionDoubleString(2.5));
            app.MapGet("/bool",     () => new UnionBoolString(true));
            app.MapGet("/guid",     () => new UnionGuidInt(new Guid("00000000-0000-0000-0000-000000000001")));
            app.MapGet("/datetime", () => new UnionDateTimeInt(new DateTime(2024, 5, 28, 10, 0, 0, DateTimeKind.Unspecified)));
            app.MapGet("/char",     () => new UnionCharInt('x'));
            app.MapGet("/string",   () => new UnionIntString("hi"));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointsFromCompilation(compilation)
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == route);

        var ctx = CreateHttpContext();
        await endpoint.RequestDelegate(ctx);
        var body = await GetResponseBodyAsync(ctx);
        Assert.Equal(200, ctx.Response.StatusCode);
        Assert.True(body == expectedBody, $"Union case at route '{route}' (JSON token kind: {tokenKind}) should serialize to {expectedBody} but got {body}.");
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
            app.MapGet("/int",    () => new UnionNullableIntString((int?)5));
            app.MapGet("/null",   () => new UnionNullableIntString((int?)null));
            app.MapGet("/string", () => new UnionNullableIntString("hi"));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var intCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/int").RequestDelegate(intCtx);
        await VerifyResponseBodyAsync(intCtx, "5");

        // TODO enable after fix https://github.com/dotnet/runtime/issues/128688
        //var nullCtx = CreateHttpContext();
        //await endpoints.Single(e => e.RoutePattern.RawText == "/null").RequestDelegate(nullCtx);
        //await VerifyResponseBodyAsync(nullCtx, "null");

        var stringCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/string").RequestDelegate(stringCtx);
        await VerifyResponseBodyAsync(stringCtx, "\"hi\"");
    }

    [Fact]
    public async Task MapAction_ReturnsObjectCaseUnion_SerializesActiveCase()
    {
        var source = """
            app.MapGet("/cat", () => new UnionPet(new Cat("Whiskers")));
            app.MapGet("/dog", () => new UnionPet(new Dog("Labrador")));
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

    [Fact]
    public async Task MapAction_ReturnsNestedUnion_SerializesInnermostCase()
    {
        var source = """
            app.MapGet("/int",    () => new UnionOuter(new UnionInner(42)));
            app.MapGet("/string", () => new UnionOuter(new UnionInner("nested")));
            app.MapGet("/bool",   () => new UnionOuter(true));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var intCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/int").RequestDelegate(intCtx);
        await VerifyResponseBodyAsync(intCtx, "42");

        var stringCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/string").RequestDelegate(stringCtx);
        await VerifyResponseBodyAsync(stringCtx, "\"nested\"");

        var boolCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/bool").RequestDelegate(boolCtx);
        await VerifyResponseBodyAsync(boolCtx, "true");
    }

    [Fact]
    public async Task MapAction_ReturnsUnion_RuntimeTypeResolvesToNearestDeclaredCase()
    {
        // UnionPet declares only Cat and Dog as cases.
        //   /declared: returns a Dog (the exact declared case type) — serialized via Dog's contract.
        //   /derived:  returns a SausageDog (derived from Dog) — STJ walks runtime type up to the
        //              nearest declared case (Dog) and serializes using Dog's contract. The
        //              SausageDog-only "Length" property is silently dropped. This mirrors
        //              [JsonPolymorphic] default behavior for unknown derived types.
        var source = """
            app.MapGet("/declared", () => new UnionPet(new Dog("Labrador")));
            app.MapGet("/derived",  () => new UnionPet(new SausageDog("Dachshund", 40.5)));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var declaredCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/declared").RequestDelegate(declaredCtx);
        await VerifyResponseBodyAsync(declaredCtx, "{\"breed\":\"Labrador\"}");

        var derivedCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/derived").RequestDelegate(derivedCtx);
        // Length is dropped — Dog's contract has only "Breed".
        await VerifyResponseBodyAsync(derivedCtx, "{\"breed\":\"Dachshund\"}");
    }

    // Ambiguous unions (multiple cases share a JSON token kind).
    [Fact]
    public async Task MapAction_ReturnsAmbiguousNumericUnion_SerializeWorksByDotNetType()
    {
        // UnionIntShort(int, short) — both cases map to JsonValueType.Number.
        // The deconstructor sees the runtime .NET type and picks the correct case unambiguously,
        // so the wire output is identical to the non-ambiguous case.
        var source = """
            app.MapGet("/int",   () => new UnionIntShort(42));
            app.MapGet("/short", () => new UnionIntShort((short)7));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var intCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/int").RequestDelegate(intCtx);
        await VerifyResponseBodyAsync(intCtx, "42");

        var shortCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/short").RequestDelegate(shortCtx);
        await VerifyResponseBodyAsync(shortCtx, "7");
    }

    [Fact]
    public async Task MapAction_ReturnsAmbiguousStringUnion_SerializeWorksByDotNetType()
    {
        // UnionDateTimeString(DateTime, string) — both cases map to JsonValueType.String.
        // Same as the numeric ambiguity case: serialize works because the deconstructor knows
        // the .NET type. Deserialization would throw "String ambiguous" without a classifier.
        var source = """
            app.MapGet("/datetime", () => new UnionDateTimeString(new DateTime(2024, 5, 28, 10, 0, 0, DateTimeKind.Unspecified)));
            app.MapGet("/string",   () => new UnionDateTimeString("hello"));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var dtCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/datetime").RequestDelegate(dtCtx);
        await VerifyResponseBodyAsync(dtCtx, "\"2024-05-28T10:00:00\"");

        var stringCtx = CreateHttpContext();
        await endpoints.Single(e => e.RoutePattern.RawText == "/string").RequestDelegate(stringCtx);
        await VerifyResponseBodyAsync(stringCtx, "\"hello\"");
    }
}
