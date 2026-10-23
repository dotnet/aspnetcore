// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public partial class SystemTextJsonOutputFormatterTest
{
    [Theory]
    [InlineData("PrimitiveByteString", "value", "42")]
    [InlineData("PrimitiveByteString", "string", "\"hi\"")]
    [InlineData("PrimitiveShortString", "value", "1234")]
    [InlineData("PrimitiveShortString", "string", "\"hi\"")]
    [InlineData("PrimitiveIntString", "value", "42")]
    [InlineData("PrimitiveIntString", "string", "\"hi\"")]
    [InlineData("PrimitiveLongString", "value", "9999999999")]
    [InlineData("PrimitiveLongString", "string", "\"hi\"")]
    [InlineData("PrimitiveDecimalString", "value", "3.14")]
    [InlineData("PrimitiveDecimalString", "string", "\"hi\"")]
    [InlineData("PrimitiveDoubleString", "value", "2.5")]
    [InlineData("PrimitiveDoubleString", "string", "\"hi\"")]
    [InlineData("PrimitiveBoolString", "value", "true")]
    [InlineData("PrimitiveBoolString", "string", "\"hi\"")]
    [InlineData("PrimitiveGuidInt", "value", "\"00000000-0000-0000-0000-000000000001\"")]
    [InlineData("PrimitiveGuidInt", "int", "42")]
    [InlineData("PrimitiveDateTimeInt", "value", "\"2024-05-28T10:00:00\"")]
    [InlineData("PrimitiveDateTimeInt", "int", "42")]
    [InlineData("PrimitiveCharInt", "value", "\"x\"")]
    [InlineData("PrimitiveCharInt", "int", "42")]
    public async Task Union_ReturnType_PrimitiveCases_SerializeActiveCase(string action, string kind, string expectedBody)
    {
        var response = await Client.GetAsync($"/Unions/{action}/{kind}");

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("AsyncTask")]
    [InlineData("AsyncValueTask")]
    public async Task Union_ReturnType_TaskAndValueTask_SerializeActiveCase(string action)
    {
        var response = await Client.GetAsync($"/Unions/{action}");
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("42", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("value", HttpStatusCode.OK, "42")]
    [InlineData("null", HttpStatusCode.NoContent, "")]
    public async Task Union_ReturnType_NullableWrapper_HandlesValueAndNull(string kind, HttpStatusCode expectedStatus, string expectedBody)
    {
        var response = await Client.GetAsync($"/Unions/NullableWrapper/{kind}");
        await response.AssertStatusCodeAsync(expectedStatus);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("int", "5")]
    [InlineData("string", "\"hi\"")]
    [InlineData("null", "null")]
    public async Task Union_ReturnType_UnionWithNullableCase_SerializesActiveCase(string kind, string expectedBody)
    {
        var response = await Client.GetAsync($"/Unions/UnionWithNullableCase/{kind}");

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("cat", "{\"name\":\"Whiskers\"}")]
    [InlineData("dog", "{\"breed\":\"Labrador\"}")]
    public async Task Union_ReturnType_ObjectCase_SerializesActiveCase(string kind, string expectedBody)
    {
        var response = await Client.GetAsync($"/Unions/ObjectCase/{kind}");
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("int", "42")]
    [InlineData("string", "\"nested\"")]
    [InlineData("bool", "true")]
    public async Task Union_ReturnType_NestedUnion_SerializesInnermostCase(string kind, string expectedBody)
    {
        var response = await Client.GetAsync($"/Unions/Nested/{kind}");
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("int", "{\"correlationId\":\"abc\",\"payload\":42}")]
    [InlineData("string", "{\"correlationId\":\"abc\",\"payload\":\"hi\"}")]
    public async Task Union_ReturnType_AsPropertyOfWrappingRecord_SerializesActiveCase(string kind, string expectedBody)
    {
        var response = await Client.GetAsync($"/Unions/Envelope/{kind}");
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }
}
