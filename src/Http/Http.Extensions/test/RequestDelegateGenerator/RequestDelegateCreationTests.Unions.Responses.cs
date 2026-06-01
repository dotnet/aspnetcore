// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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
        // UnionPet(Cat, Dog) — both cases share JsonValueType.Object, so this is also
        // an "object-bucket ambiguous" union from STJ's perspective. Serialization is
        // unambiguous because the deconstructor dispatches by .NET runtime type; the
        // ambiguity only matters on the deserialize path (where a classifier or property-
        // name dispatch is needed to pick the case).
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

    [Fact]
    public async Task MapAction_ReturnsUnionViaResultWrappers_SerializesActiveCase()
    {
        var source = """
            app.MapGet("/results-ok-int",     () => Results.Ok(new UnionIntString(42)));
            app.MapGet("/results-ok-string",  () => Results.Ok(new UnionIntString("hi")));
            app.MapGet("/typed-ok-int",       () => TypedResults.Ok(new UnionIntString(42)));
            app.MapGet("/typed-ok-string",    () => TypedResults.Ok(new UnionIntString("hi")));
            app.MapGet("/object-int",         object () => new UnionIntString(42));
            app.MapGet("/object-string",      object () => new UnionIntString("hi"));
            app.MapGet("/iresult-task",       Task<IResult> () => Task.FromResult<IResult>(TypedResults.Ok(new UnionIntString(42))));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var cases = new (string Route, string Expected)[]
        {
            ("/results-ok-int",    "42"),
            ("/results-ok-string", "\"hi\""),
            ("/typed-ok-int",      "42"),
            ("/typed-ok-string",   "\"hi\""),
            ("/object-int",        "42"),
            ("/object-string",     "\"hi\""),
            ("/iresult-task",      "42"),
        };

        foreach (var (route, expected) in cases)
        {
            var ctx = CreateHttpContext();
            await endpoints.Single(e => e.RoutePattern.RawText == route).RequestDelegate(ctx);
            var body = await GetResponseBodyAsync(ctx);
            Assert.Equal(200, ctx.Response.StatusCode);
            Assert.True(body == expected, $"Route '{route}' should serialize wrapped union to {expected} but got {body}.");
        }
    }

    [Fact]
    public async Task MapAction_ReturnsUnionInContainer_SerializesActiveCase()
    {
        // Testing a union flowing through other STJ containers (object property, array, dictionary value) 

        var source = """
            app.MapGet("/property-int",    () => new UnionEnvelope("abc", new UnionIntString(42)));
            app.MapGet("/property-string", () => new UnionEnvelope("abc", new UnionIntString("hi")));
            app.MapGet("/array",           () => new[] { new UnionIntString(1), new UnionIntString("two") });
            app.MapGet("/dictionary",      () => new Dictionary<string, UnionIntString>
            {
                ["a"] = new UnionIntString(1),
                ["b"] = new UnionIntString("two"),
            });
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var cases = new (string Route, string Expected)[]
        {
            ("/property-int",    "{\"correlationId\":\"abc\",\"payload\":42}"),
            ("/property-string", "{\"correlationId\":\"abc\",\"payload\":\"hi\"}"),
            ("/array",           "[1,\"two\"]"),
            ("/dictionary",      "{\"a\":1,\"b\":\"two\"}"),
        };

        foreach (var (route, expected) in cases)
        {
            var ctx = CreateHttpContext();
            await endpoints.Single(e => e.RoutePattern.RawText == route).RequestDelegate(ctx);
            var body = await GetResponseBodyAsync(ctx);
            Assert.Equal(200, ctx.Response.StatusCode);
            Assert.True(body == expected, $"Route '{route}' (union in container) should serialize to {expected} but got {body}.");
        }
    }

    [Fact]
    public async Task MapAction_ReturnsUnion_WithPolymorphicCaseType_EmitsDiscriminator()
    {
        // UnionAnimalString(PolyAnimal, string) has a polymorphic case type (PolyAnimal carries
        // [JsonPolymorphic] + [JsonDerivedType] for PolyCat/PolyDog). "$type" discriminator should be emitted.

        // However UnionPolyCatDog(PolyCat, PolyDog) declares the *concrete derived types* as cases.
        // STJ uses PolyCat's own contract — which has no polymorphism options of its own — so NO discriminator is emitted.

        var source = """
            app.MapGet("/cat",          () => new UnionAnimalString(new PolyCat("Whiskers")));
            app.MapGet("/dog",          () => new UnionAnimalString(new PolyDog("Labrador")));
            app.MapGet("/string",       () => new UnionAnimalString("plain"));
            app.MapGet("/concrete-cat", () => new UnionPolyCatDog(new PolyCat("Whiskers")));
            app.MapGet("/concrete-dog", () => new UnionPolyCatDog(new PolyDog("Labrador")));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var cases = new (string Route, string Expected)[]
        {
            ("/cat",          """{"$type":"cat","name":"Whiskers"}"""),
            ("/dog",          """{"$type":"dog","breed":"Labrador"}"""),
            ("/string",       "\"plain\""),
            // No "$type" — union case type is the concrete derived type, not the polymorphic base.
            ("/concrete-cat", "{\"name\":\"Whiskers\"}"),
            ("/concrete-dog", "{\"breed\":\"Labrador\"}"),
        };

        foreach (var (route, expected) in cases)
        {
            var ctx = CreateHttpContext();
            await endpoints.Single(e => e.RoutePattern.RawText == route).RequestDelegate(ctx);
            var body = await GetResponseBodyAsync(ctx);
            Assert.Equal(200, ctx.Response.StatusCode);
            Assert.True(body == expected, $"Route '{route}' (polymorphic union case) should serialize to {expected} but got {body}.");
        }
    }

    [Fact]
    public async Task MapAction_ReturnsUnion_WithCustomTypeClassifier_SerializationUnaffected()
    {
        // A user-supplied JsonTypeClassifier only affects deserialization.
        // Applying [JsonUnion(TypeClassifier = ...)] to an ambiguous union must not change the serialized output,
        // which is still dispatched by .NET runtime type via the deconstructor.

        var source = """
            app.MapGet("/int",   () => new UnionIntShortWithClassifier(42));
            app.MapGet("/short", () => new UnionIntShortWithClassifier((short)7));
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
    public async Task MapAction_ReturnsAsyncEnumerableOfUnion_StreamsActiveCasePerItem()
    {
        // Streaming endpoints: IAsyncEnumerable<TUnion>

        var source = """
            app.MapGet("/stream", () => GetUnionsAsync());

            static async IAsyncEnumerable<UnionIntString> GetUnionsAsync()
            {
                yield return new UnionIntString(1);
                await Task.Yield();
                yield return new UnionIntString("two");
                yield return new UnionIntString(3);
            }
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().Single(e => e.RoutePattern.RawText == "/stream");

        var ctx = CreateHttpContext();
        await endpoint.RequestDelegate(ctx);
        Assert.Equal(200, ctx.Response.StatusCode);
        await VerifyResponseBodyAsync(ctx, "[1,\"two\",3]");
    }

    [Fact]
    public async Task MapAction_ReturnsUnion_HonorsConfigureHttpJsonOptions()
    {
        // ConfigureHttpJsonOptions modifications (naming policy, indented output, etc.)

        var source = """
            app.MapGet("/envelope", () => new UnionEnvelope("abc", new UnionIntString(42)));
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(services =>
        {
            services.ConfigureHttpJsonOptions(o =>
            {
                o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                o.SerializerOptions.WriteIndented = true;
            });
        });
        var endpoint = GetEndpointsFromCompilation(compilation, serviceProvider: serviceProvider).OfType<RouteEndpoint>().Single(e => e.RoutePattern.RawText == "/envelope");

        var ctx = CreateHttpContext(serviceProvider);
        await endpoint.RequestDelegate(ctx);
        Assert.Equal(200, ctx.Response.StatusCode);
        var body = await GetResponseBodyAsync(ctx);

        // Two assertions instead of a brittle exact-match: snake_case applied AND indentation produced newlines.
        Assert.Contains("\"correlation_id\": \"abc\"", body);
        Assert.Contains("\"payload\": 42", body);
        Assert.Contains("\n", body);
    }
}
