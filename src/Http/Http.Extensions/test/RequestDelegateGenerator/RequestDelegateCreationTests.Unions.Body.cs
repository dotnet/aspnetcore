// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
{
    [Theory]
    [InlineData("true", "true", "Boolean")]
    [InlineData("false", "false", "Boolean")]
    [InlineData("\"hi\"", "\"hi\"", "String")]
    public async Task MapAction_UnionBody_NaturallyUnambiguousPrimitiveCases_RoundTrip(
        string requestJson, string expectedBody, string tokenKindForReadability)
    {
        var source = """
            app.MapPost("/bool-string", (UnionBoolString u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointsFromCompilation(compilation)
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/bool-string");

        var ctx = CreateHttpContextWithJson(requestJson);
        await endpoint.RequestDelegate(ctx);

        Assert.True(ctx.Response.StatusCode == 200, $"UnionBoolString body bind for {tokenKindForReadability} payload {requestJson} should return 200 but got {ctx.Response.StatusCode}.");
        await VerifyResponseBodyAsync(ctx, expectedBody);
    }

    [Theory]
    [InlineData("/byte-string",     "\"hi\"")]
    [InlineData("/short-string",    "\"hi\"")]
    [InlineData("/int-string",      "\"hi\"")]
    [InlineData("/long-string",     "\"hi\"")]
    [InlineData("/decimal-string",  "\"hi\"")]
    [InlineData("/double-string",   "\"hi\"")]
    [InlineData("/guid-int",        "\"00000000-0000-0000-0000-000000000001\"")]
    [InlineData("/datetime-int",    "\"2024-05-28T10:00:00\"")]
    [InlineData("/char-int",        "\"x\"")]
    public async Task MapAction_UnionBody_AmbiguousPrimitiveCases_WithoutClassifier_ReturnsBadRequestUnderWebDefaults(
        string route, string requestJson)
    {
        // Every (numeric, string) and (string-shaped, numeric) primitive union is silently ambiguous on the JSON String token
        // because NumberHandling.AllowReadingFromString makes the numeric case String-eligible.
        // Sending a String-token payload throws JsonException at deserialize time and
        // surfaces as HTTP 400 with EventId "InvalidJsonRequestBody".

        var source = """
            app.MapPost("/byte-string",     (UnionByteString u) => u);
            app.MapPost("/short-string",    (UnionShortString u) => u);
            app.MapPost("/int-string",      (UnionIntString u) => u);
            app.MapPost("/long-string",     (UnionLongString u) => u);
            app.MapPost("/decimal-string",  (UnionDecimalString u) => u);
            app.MapPost("/double-string",   (UnionDoubleString u) => u);
            app.MapPost("/guid-int",        (UnionGuidInt u) => u);
            app.MapPost("/datetime-int",    (UnionDateTimeInt u) => u);
            app.MapPost("/char-int",        (UnionCharInt u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointsFromCompilation(compilation)
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == route);

        var ctx = CreateHttpContextWithJson(requestJson);
        await endpoint.RequestDelegate(ctx);

        Assert.Equal(400, ctx.Response.StatusCode);
        var log = Assert.Single(TestSink.Writes, w => w.EventId.Name == "InvalidJsonRequestBody");
        var jsonException = Assert.IsType<JsonException>(log.Exception);
        Assert.Contains("ambiguous", jsonException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("union type", jsonException.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    // Numeric (Number-token) cases.
    [InlineData("/byte-string",     "42",            "42")]
    [InlineData("/short-string",    "1234",          "1234")]
    [InlineData("/int-string",      "42",            "42")]
    [InlineData("/long-string",     "9999999999",    "9999999999")]
    [InlineData("/decimal-string",  "3.14",          "3.14")]
    [InlineData("/double-string",   "2.5",           "2.5")]
    [InlineData("/guid-int",        "42",            "42")]
    [InlineData("/datetime-int",    "42",            "42")]
    [InlineData("/char-int",        "42",            "42")]
    // String-shaped (String-token) cases.
    [InlineData("/byte-string",     "\"hi\"",                                        "\"hi\"")]
    [InlineData("/short-string",    "\"hi\"",                                        "\"hi\"")]
    [InlineData("/int-string",      "\"hi\"",                                        "\"hi\"")]
    [InlineData("/long-string",     "\"hi\"",                                        "\"hi\"")]
    [InlineData("/decimal-string",  "\"hi\"",                                        "\"hi\"")]
    [InlineData("/double-string",   "\"hi\"",                                        "\"hi\"")]
    [InlineData("/guid-int",        "\"00000000-0000-0000-0000-000000000001\"",      "\"00000000-0000-0000-0000-000000000001\"")]
    [InlineData("/datetime-int",    "\"2024-05-28T10:00:00\"",                       "\"2024-05-28T10:00:00\"")]
    [InlineData("/char-int",        "\"x\"",                                         "\"x\"")]
    public async Task MapAction_UnionBody_AmbiguousPrimitiveCasesWithClassifier_RoundTrip(
        string route, string requestJson, string expectedBody)
    {
        // Same primitive-pair unions as MapAction_UnionBody_AmbiguousPrimitiveCases_WithoutClassifier_ReturnsBadRequestUnderWebDefaults,
        // but each declares a classifier that resolves the String-token ambiguity by dispatching on token shape
        // (Number -> numeric case, String -> string-shaped case).

        var source = """
            app.MapPost("/byte-string",     (UnionByteStringWithClassifier u) => u);
            app.MapPost("/short-string",    (UnionShortStringWithClassifier u) => u);
            app.MapPost("/int-string",      (UnionIntStringWithClassifier u) => u);
            app.MapPost("/long-string",     (UnionLongStringWithClassifier u) => u);
            app.MapPost("/decimal-string",  (UnionDecimalStringWithClassifier u) => u);
            app.MapPost("/double-string",   (UnionDoubleStringWithClassifier u) => u);
            app.MapPost("/guid-int",        (UnionGuidIntWithClassifier u) => u);
            app.MapPost("/datetime-int",    (UnionDateTimeIntWithClassifier u) => u);
            app.MapPost("/char-int",        (UnionCharIntWithClassifier u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointsFromCompilation(compilation)
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == route);

        var ctx = CreateHttpContextWithJson(requestJson);
        await endpoint.RequestDelegate(ctx);

        Assert.True(ctx.Response.StatusCode == 200, $"Classifier-resolved union at '{route}' with payload {requestJson} should return 200 but got {ctx.Response.StatusCode}.");
        await VerifyResponseBodyAsync(ctx, expectedBody);
    }

    [Fact]
    public async Task MapAction_UnionBody_NullableUnionWrapper_HandlesValueAndNull()
    {
        // Body analog of MapAction_ReturnsNullableUnionWrapper_HandlesValueAndNull:
        // a UnionIntString? body parameter accepts both a concrete value and a JSON null payload.

        var source = """
            app.MapPost("/maybe", (UnionIntString? u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/maybe");

        var valueCtx = CreateHttpContextWithJson("42");
        await endpoint.RequestDelegate(valueCtx);
        Assert.Equal(200, valueCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(valueCtx, "42");

        var nullCtx = CreateHttpContextWithJson("null");
        await endpoint.RequestDelegate(nullCtx);
        Assert.Equal(200, nullCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(nullCtx, "null");
    }

    [Fact]
    public async Task MapAction_UnionBody_UnionWithNullableCase_FailsWithoutClassifier_PassesWithClassifier()
    {
        // UnionNullableIntString(int?, string):
        //   Number  → only int? matches → unambiguous (works on both endpoints)
        //   String  → ambiguous (NumberHandling makes int? string-eligible)
        //   Null    → ambiguous (both cases accept null)
        // The classifier dispatches Number/Null → int and String → string.

        var source = """
            app.MapPost("/nullable-int-string",            (UnionNullableIntString u) => u);
            app.MapPost("/nullable-int-string-classifier", (UnionNullableIntStringWithClassifier u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        // Unambiguous Number token works on the plain endpoint.
        var okCtx = CreateHttpContextWithJson("42");
        await endpoints.Single(e => e.RoutePattern.RawText == "/nullable-int-string").RequestDelegate(okCtx);
        Assert.Equal(200, okCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(okCtx, "42");

        // Ambiguous String token
        var stringCtx = CreateHttpContextWithJson("\"hi\"");
        await endpoints.Single(e => e.RoutePattern.RawText == "/nullable-int-string").RequestDelegate(stringCtx);
        Assert.True(stringCtx.Response.StatusCode == 400, $"UnionNullableIntString body bind for {"\"hi\""} should return 400 but got {stringCtx.Response.StatusCode}.");

        // TODO enable after fix https://github.com/dotnet/runtime/issues/128688
        var nullCtx = CreateHttpContextWithJson("null");
        await endpoints.Single(e => e.RoutePattern.RawText == "/nullable-int-string").RequestDelegate(stringCtx);
        Assert.True(stringCtx.Response.StatusCode == 400, $"UnionNullableIntString body bind for \"null\" should return 400 but got {stringCtx.Response.StatusCode}.");

        // Classifier endpoint accepts and round-trips every token kind.
        foreach (var (payload, expected) in new[]
        {
            ("42",     "42"),
            ("\"hi\"", "\"hi\""),
            // TODO enable after fix https://github.com/dotnet/runtime/issues/128688
            // ("null",   "null"),
        })
        {
            var passCtx = CreateHttpContextWithJson(payload);
            await endpoints.Single(e => e.RoutePattern.RawText == "/nullable-int-string-classifier").RequestDelegate(passCtx);
            Assert.True(passCtx.Response.StatusCode == 200, $"Classifier-resolved UnionNullableIntString for {payload} should return 200 but got {passCtx.Response.StatusCode}.");
            await VerifyResponseBodyAsync(passCtx, expected);
        }
    }

    [Fact]
    public async Task MapAction_UnionBody_ObjectCaseUnion_FailsWithoutClassifier_PassesWithClassifier()
    {
        // UnionPet(Cat, Dog): both cases serialize to JSON objects, so the Object token is ambiguous.
        // The classifier resolves by property-name dispatch: "name" → Cat, "breed" → Dog.

        var source = """
            app.MapPost("/pet",            (UnionPet u) => u);
            app.MapPost("/pet-classifier", (UnionPetWithClassifier u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        foreach (var payload in new[]
        {
            "{\"name\":\"Whiskers\"}",
            "{\"breed\":\"Labrador\"}",
        })
        {
            var failCtx = CreateHttpContextWithJson(payload);
            await endpoints.Single(e => e.RoutePattern.RawText == "/pet").RequestDelegate(failCtx);
            Assert.True(failCtx.Response.StatusCode == 400, $"UnionPet body bind for {payload} should return 400 but got {failCtx.Response.StatusCode}.");
        }

        foreach (var (payload, expected) in new[]
        {
            ("{\"name\":\"Whiskers\"}",  "{\"name\":\"Whiskers\"}"),
            ("{\"breed\":\"Labrador\"}", "{\"breed\":\"Labrador\"}"),
        })
        {
            var passCtx = CreateHttpContextWithJson(payload);
            await endpoints.Single(e => e.RoutePattern.RawText == "/pet-classifier").RequestDelegate(passCtx);
            Assert.True(passCtx.Response.StatusCode == 200, $"UnionPetWithClassifier body bind for {payload} should return 200 but got {passCtx.Response.StatusCode}.");
            await VerifyResponseBodyAsync(passCtx, expected);
        }
    }

    [Fact]
    public async Task MapAction_UnionBody_ObjectCaseUnion_PayloadOfDerivedType_ResolvesToNearestDeclaredCase()
    {
        // UnionPet only declares Cat and Dog. A SausageDog-shaped payload (has "breed" and "length")
        // is dispatched by the classifier to Dog (it matches on "breed" first). STJ then deserializes
        // through Dog's contract — the SausageDog-only "Length" property is silently dropped.
        // Mirrors the runtime-type behavior tested in MapAction_ReturnsUnion_RuntimeTypeResolvesToNearestDeclaredCase.

        var source = """
            app.MapPost("/pet-classifier", (UnionPetWithClassifier u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/pet-classifier");

        var ctx = CreateHttpContextWithJson("{\"breed\":\"Dachshund\",\"length\":40.5}");
        await endpoint.RequestDelegate(ctx);
        Assert.Equal(200, ctx.Response.StatusCode);
        await VerifyResponseBodyAsync(ctx, "{\"breed\":\"Dachshund\"}");
    }

    [Fact]
    public async Task MapAction_UnionBody_NestedUnion_RequiresOuterClassifierToReachInner()
    {
        // UnionOuter(UnionInner, bool):
        // STJ's union deserializer does NOT recurse into nested-union cases when dispatching by
        // JSON token type. It only knows how to map primitive token types to primitive case types
        // — a nested union case like UnionInner has no associated JsonValueType, so the outer
        // converter rejects every token that would have to flow into the inner union.
        //
        //   Boolean  → bool (token-distinct, no inner involvement) → 200 round-trip
        //   Number   → would need UnionInner, but the outer can't dispatch into it → 400
        //   String   → same as Number → 400
        //
        // Adding a classifier ONLY to the inner union (UnionOuterWithClassifier) does not help,
        // because the outer converter never reaches the inner classifier.
        //
        // Adding a classifier at BOTH levels (UnionOuterWithBothClassifiers) makes the nested
        // union fully reachable: the outer classifier dispatches Number/String → UnionInner type,
        // and the inner classifier then resolves the Number/String ambiguity.

        var source = """
            app.MapPost("/outer",                  (UnionOuter u) => u);
            app.MapPost("/outer-classifier",       (UnionOuterWithClassifier u) => u);
            app.MapPost("/outer-both-classifiers", (UnionOuterWithBothClassifiers u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        // Boolean: outer dispatches directly to the bool case. Works on every endpoint.
        foreach (var route in new[] { "/outer", "/outer-classifier", "/outer-both-classifiers" })
        {
            var boolCtx = CreateHttpContextWithJson("true");
            await endpoints.Single(e => e.RoutePattern.RawText == route).RequestDelegate(boolCtx);
            Assert.Equal(200, boolCtx.Response.StatusCode);
            await VerifyResponseBodyAsync(boolCtx, "true");
        }

        // Number and String require recursion into the inner union → 400 when no outer classifier
        // routes through. The inner classifier on /outer-classifier never gets a chance to run.
        foreach (var route in new[] { "/outer", "/outer-classifier" })
        {
            foreach (var payload in new[] { "42", "\"hi\"" })
            {
                var failCtx = CreateHttpContextWithJson(payload);
                await endpoints.Single(e => e.RoutePattern.RawText == route).RequestDelegate(failCtx);
                Assert.True(failCtx.Response.StatusCode == 400, $"Nested-union body bind for {payload} on {route} should return 400 but got {failCtx.Response.StatusCode}.");
            }
        }

        // With classifiers on BOTH the outer and inner unions, Number and String round-trip
        // end-to-end through the nested case.
        foreach (var (payload, expected) in new[]
        {
            ("42",     "42"),
            ("\"hi\"", "\"hi\""),
        })
        {
            var passCtx = CreateHttpContextWithJson(payload);
            await endpoints.Single(e => e.RoutePattern.RawText == "/outer-both-classifiers").RequestDelegate(passCtx);
            Assert.True(passCtx.Response.StatusCode == 200, $"Outer+inner classifier nested-union body bind for {payload} should return 200 but got {passCtx.Response.StatusCode}.");
            await VerifyResponseBodyAsync(passCtx, expected);
        }
    }

    [Fact]
    public async Task MapAction_UnionBody_AmbiguousNumericUnion_FailsWithoutClassifier_PassesWithClassifier()
    {
        // UnionIntShort(int, short): both cases share JsonValueType.Number.
        // UnionIntShortWithClassifier uses IntFirstClassifierFactory which always returns typeof(int),
        // resolving the Number-token ambiguity to the int case unconditionally.

        var source = """
            app.MapPost("/intshort",            (UnionIntShort u) => u);
            app.MapPost("/intshort-classifier", (UnionIntShortWithClassifier u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        var failCtx = CreateHttpContextWithJson("42");
        await endpoints.Single(e => e.RoutePattern.RawText == "/intshort").RequestDelegate(failCtx);
        Assert.True(failCtx.Response.StatusCode == 400, $"UnionIntShort body bind for Number payload should return 400 but got {failCtx.Response.StatusCode}.");

        var passCtx = CreateHttpContextWithJson("42");
        await endpoints.Single(e => e.RoutePattern.RawText == "/intshort-classifier").RequestDelegate(passCtx);
        Assert.Equal(200, passCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(passCtx, "42");
    }

    [Fact]
    public async Task MapAction_UnionBody_UnionInContainer_FailsWithoutClassifier_PassesWithClassifier()
    {
        // UnionEnvelope(string CorrelationId, UnionIntString Payload): the envelope itself is unambiguous;
        // the inner union ambiguity surfaces only when the Payload value is a String-token (ambiguous via NumberHandling).
        // The WithClassifier envelope wraps UnionIntStringWithClassifier instead, resolving the inner ambiguity.

        var source = """
            app.MapPost("/envelope",            (UnionEnvelope u) => u);
            app.MapPost("/envelope-classifier", (UnionEnvelopeWithClassifier u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToArray();

        // Number-token Payload: no inner ambiguity → both endpoints round-trip.
        foreach (var route in new[] { "/envelope", "/envelope-classifier" })
        {
            var ctx = CreateHttpContextWithJson("{\"correlationId\":\"abc\",\"payload\":42}");
            await endpoints.Single(e => e.RoutePattern.RawText == route).RequestDelegate(ctx);
            Assert.Equal(200, ctx.Response.StatusCode);
            await VerifyResponseBodyAsync(ctx, "{\"correlationId\":\"abc\",\"payload\":42}");
        }

        // String-token Payload: inner UnionIntString ambiguous → 400 on the plain envelope.
        var failCtx = CreateHttpContextWithJson("{\"correlationId\":\"abc\",\"payload\":\"hi\"}");
        await endpoints.Single(e => e.RoutePattern.RawText == "/envelope").RequestDelegate(failCtx);
        Assert.True(failCtx.Response.StatusCode == 400, $"UnionEnvelope body bind for String Payload should return 400 but got {failCtx.Response.StatusCode}.");

        // Resolves string via the classifier.
        var passCtx = CreateHttpContextWithJson("{\"correlationId\":\"abc\",\"payload\":\"hi\"}");
        await endpoints.Single(e => e.RoutePattern.RawText == "/envelope-classifier").RequestDelegate(passCtx);
        Assert.Equal(200, passCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(passCtx, "{\"correlationId\":\"abc\",\"payload\":\"hi\"}");
    }

    [Fact]
    public async Task MapAction_UnionBody_PolymorphicCaseUnion_BindsActiveCase()
    {
        // UnionAnimalString(PolyAnimal, string): Object (PolyAnimal) vs String (string) → token-distinct.
        // PolyAnimal carries [JsonPolymorphic] + [JsonDerivedType] so the concrete derived case (PolyCat/PolyDog)
        // is discovered via the "$type" discriminator on the deserialize path.

        var source = """
            app.MapPost("/animal", (UnionAnimalString u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/animal");

        foreach (var (payload, expected) in new[]
        {
            ("{\"$type\":\"cat\",\"name\":\"Whiskers\"}", "{\"$type\":\"cat\",\"name\":\"Whiskers\"}"),
            ("{\"$type\":\"dog\",\"breed\":\"Labrador\"}", "{\"$type\":\"dog\",\"breed\":\"Labrador\"}"),
            ("\"plain\"",                                  "\"plain\""),
        })
        {
            var ctx = CreateHttpContextWithJson(payload);
            await endpoint.RequestDelegate(ctx);
            Assert.True(ctx.Response.StatusCode == 200, $"UnionAnimalString body bind for {payload} should return 200 but got {ctx.Response.StatusCode}.");
            await VerifyResponseBodyAsync(ctx, expected);
        }
    }

    [Fact]
    public async Task MapAction_UnionBody_HonorsConfigureHttpJsonOptions()
    {
        // ConfigureHttpJsonOptions (snake_case naming policy + WriteIndented) flows through the body
        // binding path and is honored on both the deserialize and the serialize side.

        var source = """
            app.MapPost("/envelope", (UnionEnvelope u) => u);
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
        var endpoint = GetEndpointsFromCompilation(compilation, serviceProvider: serviceProvider)
            .OfType<RouteEndpoint>().Single(e => e.RoutePattern.RawText == "/envelope");

        var ctx = CreateHttpContextWithJson("{\"correlation_id\":\"abc\",\"payload\":42}", serviceProvider);
        await endpoint.RequestDelegate(ctx);
        Assert.Equal(200, ctx.Response.StatusCode);
        var body = await GetResponseBodyAsync(ctx);

        // Two assertions instead of a brittle exact-match: snake_case applied AND indentation produced newlines.
        Assert.Contains("\"correlation_id\": \"abc\"", body);
        Assert.Contains("\"payload\": 42", body);
        Assert.Contains("\n", body);
    }

    [Fact]
    public async Task MapAction_UnionBody_ExplicitFromBody_BehavesLikeImplicitBody()
    {
        // [FromBody] on a union parameter should produce the same binding behavior as the
        // implicit body inference. Both the happy-path (unambiguous Number) and the ambiguous
        // (String) cases should match.

        var source = """
            app.MapPost("/implicit", (UnionIntString u) => u);
            app.MapPost("/explicit", ([FromBody] UnionIntString u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToList();
        var implicitEndpoint = endpoints.Single(e => e.RoutePattern.RawText == "/implicit");
        var explicitEndpoint = endpoints.Single(e => e.RoutePattern.RawText == "/explicit");

        var cases = new (string Payload, int ExpectedStatus, string ExpectedBody)[]
        {
            ("42", 200, "42"),
            ("\"hi\"", 400, string.Empty), // String token is ambiguous for UnionIntString
        };

        foreach (var (payload, expectedStatus, expectedBody) in cases)
        {
            var implicitCtx = CreateHttpContextWithJson(payload);
            await implicitEndpoint.RequestDelegate(implicitCtx);
            Assert.True(implicitCtx.Response.StatusCode == expectedStatus, $"/implicit with payload {payload} expected {expectedStatus} but got {implicitCtx.Response.StatusCode}.");
            if (expectedStatus == 200)
            {
                await VerifyResponseBodyAsync(implicitCtx, expectedBody);
            }

            var explicitCtx = CreateHttpContextWithJson(payload);
            await explicitEndpoint.RequestDelegate(explicitCtx);
            Assert.True(explicitCtx.Response.StatusCode == expectedStatus, $"/explicit with payload {payload} expected {expectedStatus} but got {explicitCtx.Response.StatusCode}.");
            if (expectedStatus == 200)
            {
                await VerifyResponseBodyAsync(explicitCtx, expectedBody);
            }
        }
    }

    [Fact]
    public async Task MapAction_UnionBody_EmptyBody_ReturnsExpectedStatusPerNullabilityAndPolicy()
    {
        // Three flavors of "what should the union parameter do when no request body is provided":
        //  1. (UnionIntString u)                                                       — required → 400 (handler not invoked)
        //  2. (UnionIntString? u)                                                      — nullable → 200, u is null
        //  3. ([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UnionDoubleString u) — explicit allow → 200, u is default

        // NOTE: `/allow-empty` intentionally uses a DIFFERENT union type (UnionDoubleString) than the other two endpoints
        // see https://github.com/dotnet/aspnetcore/issues/66912

        var source = """
            app.MapPost("/required",    (UnionIntString u) => "invoked");
            app.MapPost("/nullable",    (UnionIntString? u) => u.HasValue ? "has-value" : "null");
            app.MapPost("/allow-empty", ([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UnionDoubleString u) => "invoked");
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToList();
        var requiredEndpoint   = endpoints.Single(e => e.RoutePattern.RawText == "/required");
        var nullableEndpoint   = endpoints.Single(e => e.RoutePattern.RawText == "/nullable");
        var allowEmptyEndpoint = endpoints.Single(e => e.RoutePattern.RawText == "/allow-empty");

        var requiredCtx = CreateHttpContextWithEmptyJsonBody();
        await requiredEndpoint.RequestDelegate(requiredCtx);
        Assert.Equal(400, requiredCtx.Response.StatusCode);

        // NOTE: response body differs across paths for `(UnionIntString? u)` with empty body:
        // for runtime and source generator paths: https://github.com/dotnet/aspnetcore/issues/57055
        var nullableCtx = CreateHttpContextWithEmptyJsonBody();
        await nullableEndpoint.RequestDelegate(nullableCtx);
        Assert.Equal(200, nullableCtx.Response.StatusCode);

        var allowEmptyCtx = CreateHttpContextWithEmptyJsonBody();
        await allowEmptyEndpoint.RequestDelegate(allowEmptyCtx);
        Assert.Equal(200, allowEmptyCtx.Response.StatusCode);
    }

    [Fact]
    public async Task MapAction_UnionBody_NonJsonContentType_Returns415()
    {
        // The body-bind path rejects non-JSON content types with 415, mirroring the behavior
        // for ordinary complex body parameters. Asserts:
        //   - application/json (baseline)         → 200
        //   - text/plain                          → 415
        //   - (no Content-Type header at all)     → 415

        var source = """
            app.MapPost("/", (UnionIntString u) => u);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().Single();

        var baselineCtx = CreateHttpContextWithJson("42");
        await endpoint.RequestDelegate(baselineCtx);
        Assert.Equal(200, baselineCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(baselineCtx, "42");

        var textPlainCtx = CreateHttpContextWithCustomContentType("42", contentType: "text/plain");
        await endpoint.RequestDelegate(textPlainCtx);
        Assert.Equal(415, textPlainCtx.Response.StatusCode);

        var noContentTypeCtx = CreateHttpContextWithCustomContentType("42", contentType: null);
        await endpoint.RequestDelegate(noContentTypeCtx);
        Assert.Equal(415, noContentTypeCtx.Response.StatusCode);
    }

    [Fact]
    public async Task MapAction_UnionBody_NestedInWrapperDto_DeserializesUnionProperty()
    {
        // UnionEnvelope wraps a UnionIntString as a property.

        var source = """
            app.MapPost("/envelope",            (UnionEnvelope e) => e);
            app.MapPost("/envelope-classifier", (UnionEnvelopeWithClassifier e) => e);
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToList();
        var bareEndpoint       = endpoints.Single(e => e.RoutePattern.RawText == "/envelope");
        var classifierEndpoint = endpoints.Single(e => e.RoutePattern.RawText == "/envelope-classifier");

        var bareIntCtx = CreateHttpContextWithJson("""{"correlationId":"abc","payload":42}""");
        await bareEndpoint.RequestDelegate(bareIntCtx);
        Assert.Equal(200, bareIntCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(bareIntCtx, """{"correlationId":"abc","payload":42}""");

        // ambiguous for string case without classifier
        var bareStringCtx = CreateHttpContextWithJson("""{"correlationId":"abc","payload":"hi"}""");
        await bareEndpoint.RequestDelegate(bareStringCtx);
        Assert.Equal(400, bareStringCtx.Response.StatusCode);

        var classifierIntCtx = CreateHttpContextWithJson("""{"correlationId":"abc","payload":42}""");
        await classifierEndpoint.RequestDelegate(classifierIntCtx);
        Assert.Equal(200, classifierIntCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(classifierIntCtx, """{"correlationId":"abc","payload":42}""");

        var classifierStringCtx = CreateHttpContextWithJson("""{"correlationId":"abc","payload":"hi"}""");
        await classifierEndpoint.RequestDelegate(classifierStringCtx);
        Assert.Equal(200, classifierStringCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(classifierStringCtx, """{"correlationId":"abc","payload":"hi"}""");
    }

    [Fact]
    public async Task MapAction_UnionBody_AsParametersContainer_BindsUnionFromBodyAndRouteFromRoute()
    {
        // [AsParameters] unwraps the container and binds each property: union → body, TenantId → route

        var source = """
            app.MapPost("/tenants/{TenantId}",            ([AsParameters] UnionAsParametersList args)               => $"{args.TenantId}:{args.Payload.Value}");
            app.MapPost("/classifier-tenants/{TenantId}", ([AsParameters] UnionWithClassifierAsParametersList args) => $"{args.TenantId}:{args.Payload.Value}");
        """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation).OfType<RouteEndpoint>().ToList();
        var bareEndpoint       = endpoints.Single(e => e.RoutePattern.RawText == "/tenants/{TenantId}");
        var classifierEndpoint = endpoints.Single(e => e.RoutePattern.RawText == "/classifier-tenants/{TenantId}");

        var bareIntCtx = CreateHttpContextWithJson("42");
        bareIntCtx.Request.RouteValues["TenantId"] = "7";
        await bareEndpoint.RequestDelegate(bareIntCtx);
        Assert.Equal(200, bareIntCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(bareIntCtx, "7:42");

        // ambiguous for string case without classifier
        var bareStringCtx = CreateHttpContextWithJson("\"hi\"");
        bareStringCtx.Request.RouteValues["TenantId"] = "9";
        await bareEndpoint.RequestDelegate(bareStringCtx);
        Assert.Equal(400, bareStringCtx.Response.StatusCode);

        var classifierIntCtx = CreateHttpContextWithJson("42");
        classifierIntCtx.Request.RouteValues["TenantId"] = "7";
        await classifierEndpoint.RequestDelegate(classifierIntCtx);
        Assert.Equal(200, classifierIntCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(classifierIntCtx, "7:42");

        var classifierStringCtx = CreateHttpContextWithJson("\"hi\"");
        classifierStringCtx.Request.RouteValues["TenantId"] = "9";
        await classifierEndpoint.RequestDelegate(classifierStringCtx);
        Assert.Equal(200, classifierStringCtx.Response.StatusCode);
        await VerifyResponseBodyAsync(classifierStringCtx, "9:hi");
    }
}
