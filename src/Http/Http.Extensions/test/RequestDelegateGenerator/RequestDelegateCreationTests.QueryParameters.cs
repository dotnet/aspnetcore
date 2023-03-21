// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests
{
    public static object[][] MapAction_ExplicitQueryParam_StringReturn_Data
    {
        get
        {
            var expectedBody = "TestQueryValue";
            var fromQueryRequiredSource = """app.MapGet("/", ([FromQuery] string queryValue) => queryValue);""";
            var fromQueryWithNameRequiredSource = """app.MapGet("/", ([FromQuery(Name = "queryValue")] string parameterName) => parameterName);""";
            var fromQueryWithNullNameRequiredSource = """app.MapGet("/", ([FromQuery(Name = null)] string queryValue) => queryValue);""";
            var fromQueryNullableSource = """app.MapGet("/", ([FromQuery] string? queryValue) => queryValue ?? string.Empty);""";
            var fromQueryDefaultValueSource = """
#nullable disable
string getQueryWithDefault([FromQuery] string queryValue = null) => queryValue ?? string.Empty;
app.MapGet("/", getQueryWithDefault);
#nullable restore
""";

            return new[]
            {
                new object[] { fromQueryRequiredSource, expectedBody, 200, expectedBody },
                new object[] { fromQueryRequiredSource, null, 400, string.Empty },
                new object[] { fromQueryWithNameRequiredSource, expectedBody, 200, expectedBody },
                new object[] { fromQueryWithNameRequiredSource, null, 400, string.Empty },
                new object[] { fromQueryWithNullNameRequiredSource, expectedBody, 200, expectedBody },
                new object[] { fromQueryWithNullNameRequiredSource, null, 400, string.Empty },
                new object[] { fromQueryNullableSource, expectedBody, 200, expectedBody },
                new object[] { fromQueryNullableSource, null, 200, string.Empty },
                new object[] { fromQueryDefaultValueSource, expectedBody, 200, expectedBody },
                new object[] { fromQueryDefaultValueSource, null, 200, string.Empty },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_ExplicitQueryParam_StringReturn_Data))]
    public async Task MapAction_ExplicitQueryParam_StringReturn(string source, string queryValue, int expectedStatusCode, string expectedBody)
    {
        var (results, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, (endpointModel) =>
        {
            Assert.Equal("/", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            var p = Assert.Single(endpointModel.Parameters);
            Assert.Equal(EndpointParameterSource.Query, p.Source);
            Assert.Equal("queryValue", p.LookupName);
        });

        var httpContext = CreateHttpContext();
        if (queryValue is not null)
        {
            httpContext.Request.QueryString = new QueryString($"?queryValue={queryValue}");
        }

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody, expectedStatusCode);
    }

    [Fact]
    public async Task MapAction_SingleNullableStringParam_WithEmptyQueryStringValueProvided_StringReturn()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromQuery]string? p) => p == string.Empty ? "No value, but not null!" : "Was null!");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("/hello", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            var p = Assert.Single(endpointModel.Parameters);
            Assert.Equal(EndpointParameterSource.Query, p.Source);
            Assert.Equal("p", p.SymbolName);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "No value, but not null!");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_MultipleStringParam_StringReturn()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromQuery]string p1, [FromQuery]string p2) => $"{p1} {p2}");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("/hello", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p1=Hello&p2=world!");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    public static object[][] MapAction_ExplicitQueryParam_NameTest_Data
    {
        get
        {

            return new[]
            {
                new object[] { "name", "name" },
                new object[] { "_", "_" },
                new object[] { "123", "123" },
                new object[] { "💩", "💩" },
                new object[] { "\r", "\\r" },
                new object[] { "\x00E7" , "\x00E7" },
                new object[] { "!!" , "!!" },
                new object[] { "\\" , "\\\\" },
                new object[] { "\'" , "\'" },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_ExplicitQueryParam_NameTest_Data))]
    public async Task MapAction_ExplicitQueryParam_NameTest(string name, string lookupName)
    {
        var (results, compilation) = await RunGeneratorAsync($"""app.MapGet("/", ([FromQuery(Name = @"{name}")] string queryValue) => queryValue);""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, (endpointModel) =>
        {
            Assert.Equal("/", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            var p = Assert.Single(endpointModel.Parameters);
            Assert.Equal(EndpointParameterSource.Query, p.Source);
            Assert.Equal(lookupName, p.LookupName);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString($"?{name}=test");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "test", 200);
    }
}
