// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Routing;

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

        Assert.True(ctx.Response.StatusCode == 200,
            $"Classifier-resolved union at '{route}' with payload {requestJson} should return 200 but got {ctx.Response.StatusCode}.");
        await VerifyResponseBodyAsync(ctx, expectedBody);
    }
}
