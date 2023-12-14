// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests
{
    public static object[][] MapAction_ExplicitRouteParam_SimpleReturn_Data
    {
        get
        {
            var expectedBody = "Test route value";
            var fromRouteRequiredSource = """app.MapGet("/{routeValue}", ([FromRoute] string routeValue) => routeValue);""";
            var fromRouteWithNameRequiredSource = """app.MapGet("/{routeValue}", ([FromRoute(Name = "routeValue" )] string parameterName) => parameterName);""";
            var fromRouteWithNullNameRequiredSource = """app.MapGet("/{routeValue}", ([FromRoute(Name = null )] string routeValue) => routeValue);""";
            var fromRouteNullableSource = """app.MapGet("/{routeValue}", ([FromRoute] string? routeValue) => routeValue ?? string.Empty);""";
            var fromRouteDefaultValueSource = """
#nullable disable
string getRouteWithDefault([FromRoute] string routeValue = null) => routeValue ?? string.Empty;
app.MapGet("/{routeValue}", getRouteWithDefault);
#nullable restore
""";

            return new[]
            {
                new object[] { fromRouteRequiredSource, expectedBody, 200, expectedBody },
                new object[] { fromRouteRequiredSource, null, 400, string.Empty },
                new object[] { fromRouteWithNameRequiredSource, expectedBody, 200, expectedBody },
                new object[] { fromRouteWithNameRequiredSource, null, 400, string.Empty },
                new object[] { fromRouteWithNullNameRequiredSource, expectedBody, 200, expectedBody },
                new object[] { fromRouteWithNullNameRequiredSource, null, 400, string.Empty },
                new object[] { fromRouteNullableSource, expectedBody, 200, expectedBody },
                new object[] { fromRouteNullableSource, null, 200, string.Empty },
                new object[] { fromRouteDefaultValueSource, expectedBody, 200, expectedBody },
                new object[] { fromRouteDefaultValueSource, null, 200, string.Empty },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_ExplicitRouteParam_SimpleReturn_Data))]
    public async Task MapAction_ExplicitRouteParam_SimpleReturn(string source, string requestData, int expectedStatusCode, string expectedBody)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        if (requestData is not null)
        {
            httpContext.Request.RouteValues["routeValue"] = requestData;
        }

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody, expectedStatusCode);
    }

    public static object[][] MapAction_RouteOrQueryParam_SimpleReturn_Data
    {
        get
        {
            var expectedBody = "ValueFromRouteOrQuery";
            var implicitRouteRequiredSource = """app.MapGet("/{value}", (string value) => value);""";
            var implicitQueryRequiredSource = """app.MapGet("", (string value) => value);""";
            var implicitRouteNullableSource = """app.MapGet("/{value}", (string? value) => value ?? string.Empty);""";
            var implicitQueryNullableSource = """app.MapGet("/", (string? value) => value ?? string.Empty);""";
            var implicitRouteDefaultValueSource = """
#nullable disable
string getRouteWithDefault(string value = null) => value ?? string.Empty;
app.MapGet("/{value}", getRouteWithDefault);
#nullable restore
""";

            var implicitQueryDefaultValueSource = """
#nullable disable
string getQueryWithDefault(string value = null) => value ?? string.Empty;
app.MapGet("/", getQueryWithDefault);
#nullable restore
""";

            return new[]
            {
                new object[] { implicitRouteRequiredSource, true, false, 200, expectedBody },
                new object[] { implicitRouteRequiredSource, false, false, 400, string.Empty },
                new object[] { implicitQueryRequiredSource, false, true, 200, expectedBody },
                new object[] { implicitQueryRequiredSource, false, false, 400, string.Empty },

                new object[] { implicitRouteNullableSource, true, false, 200, expectedBody },
                new object[] { implicitRouteNullableSource, false, false, 200, string.Empty },
                new object[] { implicitQueryNullableSource, false, true, 200, expectedBody },
                new object[] { implicitQueryNullableSource, false, false, 200, string.Empty },

                new object[] { implicitRouteDefaultValueSource, true, false, 200, expectedBody },
                new object[] { implicitRouteDefaultValueSource, false, false, 200, string.Empty },
                new object[] { implicitQueryDefaultValueSource, false, true, 200, expectedBody },
                new object[] { implicitQueryDefaultValueSource, false, false, 200, string.Empty },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_RouteOrQueryParam_SimpleReturn_Data))]
    public async Task MapAction_RouteOrQueryParam_SimpleReturn(string source, bool hasRoute, bool hasQuery, int expectedStatusCode, string expectedBody)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        if (hasRoute)
        {
            httpContext.Request.RouteValues["value"] = expectedBody;
        }

        if (hasQuery)
        {
            httpContext.Request.QueryString = new QueryString($"?value={expectedBody}");
        }

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody, expectedStatusCode);
    }

    [Fact]
    public async Task SpecifiedQueryParametersDoNotFallbackToRouteValues()
    {
        var source = """
app.MapGet("/", (string value, HttpContext httpContext) => value);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["value"] = "fromRoute";
        httpContext.Request.QueryString = new QueryString($"?value=fromQuery");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "fromQuery");
    }

    [Fact]
    public async Task SpecifiedRouteParametersDoNotFallbackToQueryString()
    {
        var source = """
app.MapGet("/{value}", (string value, HttpContext httpContext) => value);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["value"] = "fromRoute";
        httpContext.Request.QueryString = new QueryString($"?value=fromQuery");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "fromRoute");
    }

    public static object[][] MapAction_ExplicitRouteParam_RouteNames_Data
    {
        get
        {
            return new[]
            {
                new object[] { "name" },
                new object[] { "_" },
                new object[] { "123" },
                new object[] { "💩" },
                new object[] { "\r" },
                new object[] { "\x00E7"  },
                new object[] { "!!" },
                new object[] { "\\" },
                new object[] { "\'" },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_ExplicitRouteParam_RouteNames_Data))]
    public async Task MapAction_ExplicitRouteParam_RouteNames(string routeParameterName)
    {
        var (_, compilation) = await RunGeneratorAsync($$"""app.MapGet(@"/{{{routeParameterName}}}", ([FromRoute(Name=@"{{routeParameterName}}")] string routeValue) => routeValue);""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues[routeParameterName] = "test";

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "test", 200);
    }

    [Fact]
    public async Task Returns400IfNoMatchingRouteValueForRequiredParam()
    {
        var (_, compilation) = await RunGeneratorAsync($$"""app.MapGet(@"/{foo}", (int foo) => foo);""");
        var endpoint = GetEndpointFromCompilation(compilation);

        const string unmatchedName = "value";
        const int unmatchedRouteParam = 42;

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues[unmatchedName] = unmatchedRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        var requestDelegate = endpoint.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromRouteParameterBasedOnParameterName()
    {
        const string paramName = "value";
        const int originalRouteParam = 42;

        var (_, compilation) = await RunGeneratorAsync(
            $$"""
            app.MapGet(@"/{{{paramName}}}", (HttpContext httpContext, [FromRoute] int value) => {
                httpContext.Items.Add("input", value);
            });
            """);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        var requestDelegate = endpoint.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(originalRouteParam, httpContext.Items["input"]);
    }
}
