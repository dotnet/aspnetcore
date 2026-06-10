// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public partial class SystemTextJsonInputFormatterTest
{
    private static StringContent JsonBody(string json) => new(json, Encoding.UTF8, "application/json");

    [Theory]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("\"hi\"", "\"hi\"")]
    public async Task Union_Body_UnambiguousPrimitiveCases_RoundTrip(string payload, string expectedBody)
    {
        var response = await Client.PostAsync("/Unions/EchoBoolString", JsonBody(payload));
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Union_Body_AmbiguousPrimitiveCase_WithoutClassifier_ReturnsBadRequest()
    {
        var response = await Client.PostAsync("/Unions/EchoIntString", JsonBody("\"hi\""));
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("42", "42")]
    [InlineData("\"hi\"", "\"hi\"")]
    public async Task Union_Body_AmbiguousPrimitiveCase_WithClassifier_RoundTrips(string payload, string expectedBody)
    {
        var response = await Client.PostAsync("/Unions/EchoIntStringWithClassifier", JsonBody(payload));
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Union_Body_AmbiguousNumericUnion_WithoutClassifier_ReturnsBadRequest()
    {
        var response = await Client.PostAsync("/Unions/EchoIntShort", JsonBody("42"));
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Union_Body_AmbiguousNumericUnion_WithClassifier_RoundTrips()
    {
        var response = await Client.PostAsync("/Unions/EchoIntShortWithClassifier", JsonBody("42"));
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("42", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Union_Body_UnionWithNullableCase_NumberTokenWorksWithoutClassifier()
    {
        var response = await Client.PostAsync("/Unions/EchoNullableIntString", JsonBody("42"));
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("42", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Union_Body_UnionWithNullableCase_StringTokenAmbiguousWithoutClassifier_ReturnsBadRequest()
    {
        var response = await Client.PostAsync("/Unions/EchoNullableIntString", JsonBody("\"hi\""));
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("42", "42")]
    [InlineData("\"hi\"", "\"hi\"")]
    [InlineData("null", "null")]
    public async Task Union_Body_UnionWithNullableCase_WithClassifier_RoundTrips(string payload, string expectedBody)
    {
        var response = await Client.PostAsync("/Unions/EchoNullableIntStringWithClassifier", JsonBody(payload));

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Union_Body_UnionWithNullableCase_NullPayloadWorksWithoutClassifier()
    {
        // Null tokens are short-circuited in the union converter before the classifier
        // runs, so JSON null round-trips even when no classifier is registered.
        var response = await Client.PostAsync("/Unions/EchoNullableIntString", JsonBody("null"));

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("null", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("{\"name\":\"Whiskers\"}")]
    [InlineData("{\"breed\":\"Labrador\"}")]
    public async Task Union_Body_ObjectCaseUnion_WithoutClassifier_ReturnsBadRequest(string payload)
    {
        var response = await Client.PostAsync("/Unions/EchoPet", JsonBody(payload));
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("{\"name\":\"Whiskers\"}", "{\"name\":\"Whiskers\"}")]
    [InlineData("{\"breed\":\"Labrador\"}", "{\"breed\":\"Labrador\"}")]
    public async Task Union_Body_ObjectCaseUnion_WithClassifier_RoundTrips(string payload, string expectedBody)
    {
        var response = await Client.PostAsync("/Unions/EchoPetWithClassifier", JsonBody(payload));
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Union_Body_AsPropertyOfWrappingRecord_NumberPayload_RoundTrips()
    {
        const string payload = "{\"correlationId\":\"abc\",\"payload\":42}";
        var response = await Client.PostAsync("/Unions/EchoEnvelope", JsonBody(payload));
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(payload, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("{\"correlationId\":\"abc\",\"payload\":null}")]
    [InlineData("{\"correlationId\":\"abc\"}")]
    public async Task Union_Body_AsPropertyOfWrappingRecord_NullOrMissingPayload_RoundTripsAsNull(string payload)
    {
        var response = await Client.PostAsync("/Unions/EchoEnvelope", JsonBody(payload));
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("{\"correlationId\":\"abc\",\"payload\":null}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Union_Body_ExplicitFromBodyAndImplicitBody_BehaveTheSame()
    {
        var explicitResponse = await Client.PostAsync("/Unions/EchoIntString", JsonBody("42"));
        var implicitResponse = await Client.PostAsync("/Unions/EchoIntStringImplicit", JsonBody("42"));

        await explicitResponse.AssertStatusCodeAsync(HttpStatusCode.OK);
        await implicitResponse.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("42", await explicitResponse.Content.ReadAsStringAsync());
        Assert.Equal("42", await implicitResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Union_Body_NonJsonContentType_Returns415UnsupportedMediaType()
    {
        var content = new StringContent("42", Encoding.UTF8, "text/plain");
        var response = await Client.PostAsync("/Unions/EchoIntString", content);

        await response.AssertStatusCodeAsync(HttpStatusCode.UnsupportedMediaType);
    }
}
