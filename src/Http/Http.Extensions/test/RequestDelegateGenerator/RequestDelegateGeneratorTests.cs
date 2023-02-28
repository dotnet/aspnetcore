// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System;
using System.Globalization;
using System.Net.Sockets;
using System.Net;
using System.Numerics;
using System.Reflection.Metadata;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateGeneratorTests : RequestDelegateGeneratorTestBase
{
    [Theory]
    [InlineData("HttpContext")]
    [InlineData("HttpRequest")]
    [InlineData("HttpResponse")]
    [InlineData("System.IO.Pipelines.PipeReader")]
    [InlineData("System.IO.Stream")]
    [InlineData("System.Security.Claims.ClaimsPrincipal")]
    [InlineData("System.Threading.CancellationToken")]
    public async Task MapAction_SingleSpecialTypeParam_StringReturn(string parameterType)
    {
        var (results, compilation) = await RunGeneratorAsync($"""
app.MapGet("/hello", ({parameterType} p) => p == null ? "null!" : "Hello world!");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("/hello", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            var p = Assert.Single(endpointModel.Parameters);
            Assert.Equal(EndpointParameterSource.SpecialType, p.Source);
            Assert.Equal("p", p.Name);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
    }

    [Fact]
    public async Task MapAction_MultipleSpecialTypeParam_StringReturn()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (HttpRequest req, HttpResponse res) => req is null || res is null ? "null!" : "Hello world!");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("/hello", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);

            Assert.Collection(endpointModel.Parameters,
                reqParam =>
                {
                    Assert.Equal(EndpointParameterSource.SpecialType, reqParam.Source);
                    Assert.Equal("req", reqParam.Name);
                },
                reqParam =>
                {
                    Assert.Equal(EndpointParameterSource.SpecialType, reqParam.Source);
                    Assert.Equal("res", reqParam.Name);
                });
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_MultilineLambda()
    {
        var source = """
app.MapGet("/hello", () =>
{
    return "Hello world!";
});
""";
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("/hello", endpointModel.RoutePattern);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
    }

    [Fact]
    public async Task Multiple_MapAction_NoParam_StringReturn()
    {
        var source = """
app.MapGet("/en", () => "Hello world!");
app.MapGet("/es", () => "Hola mundo!");
app.MapGet("/en-task", () => Task.FromResult("Hello world!"));
app.MapGet("/es-task", () => new ValueTask<string>("Hola mundo!"));
""";
        var (_, compilation) = await RunGeneratorAsync(source);

        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task Multiple_MapAction_WithParams_StringReturn()
    {
        var source = """
app.MapGet("/en", (HttpRequest req) => "Hello world!");
app.MapGet("/es", (HttpResponse res) => "Hola mundo!");
app.MapGet("/zh", (HttpRequest req, HttpResponse res) => "你好世界！");
""";
        var (results, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        await VerifyAgainstBaselineUsingFile(compilation);
        VerifyStaticEndpointModels(results, endpointModels => Assert.Collection(endpointModels,
            endpointModel =>
            {
                Assert.Equal("/en", endpointModel.RoutePattern);
                Assert.Equal("MapGet", endpointModel.HttpMethod);
                var reqParam = Assert.Single(endpointModel.Parameters);
                Assert.Equal(EndpointParameterSource.SpecialType, reqParam.Source);
                Assert.Equal("req", reqParam.Name);
            },
            endpointModel =>
            {
                Assert.Equal("/es", endpointModel.RoutePattern);
                Assert.Equal("MapGet", endpointModel.HttpMethod);
                var reqParam = Assert.Single(endpointModel.Parameters);
                Assert.Equal(EndpointParameterSource.SpecialType, reqParam.Source);
                Assert.Equal("res", reqParam.Name);
            },
            endpointModel =>
            {
                Assert.Equal("/zh", endpointModel.RoutePattern);
                Assert.Equal("MapGet", endpointModel.HttpMethod);
                Assert.Collection(endpointModel.Parameters,
                    reqParam =>
                    {
                        Assert.Equal(EndpointParameterSource.SpecialType, reqParam.Source);
                        Assert.Equal("req", reqParam.Name);
                    },
                    reqParam =>
                    {
                        Assert.Equal(EndpointParameterSource.SpecialType, reqParam.Source);
                        Assert.Equal("res", reqParam.Name);
                    });
            }));

        Assert.Equal(3, endpoints.Length);
        var httpContext = CreateHttpContext();
        await endpoints[0].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");

        httpContext = CreateHttpContext();
        await endpoints[1].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hola mundo!");

        httpContext = CreateHttpContext();
        await endpoints[2].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "你好世界！");
    }

    public static object[][] MapAction_ExplicitHeaderParam_SimpleReturn_Data
    {
        get
        {
            var expectedBody = "Test header value";
            var fromHeaderRequiredSource = """app.MapGet("/", ([FromHeader] string headerValue) => headerValue);""";
            var fromHeaderWithNameRequiredSource = """app.MapGet("/", ([FromHeader(Name = "headerValue")] string parameterName) => parameterName);""";
            var fromHeaderWithNullNameRequiredSource = """app.MapGet("/", ([FromHeader(Name = null)] string headerValue) => headerValue);""";
            var fromHeaderNullableSource = """app.MapGet("/", ([FromHeader] string? headerValue) => headerValue ?? string.Empty);""";
            var fromHeaderDefaultValueSource = """
#nullable disable
string getHeaderWithDefault([FromHeader] string headerValue = null) => headerValue ?? string.Empty;
app.MapGet("/", getHeaderWithDefault);
#nullable restore
""";

            return new[]
            {
                new object[] { fromHeaderRequiredSource, expectedBody, 200, expectedBody },
                new object[] { fromHeaderRequiredSource, null, 400, string.Empty },
                new object[] { fromHeaderWithNameRequiredSource, expectedBody, 200, expectedBody },
                new object[] { fromHeaderWithNameRequiredSource, null, 400, string.Empty },
                new object[] { fromHeaderWithNullNameRequiredSource, expectedBody, 200, expectedBody },
                new object[] { fromHeaderWithNullNameRequiredSource, null, 400, string.Empty },
                new object[] { fromHeaderNullableSource, expectedBody, 200, expectedBody },
                new object[] { fromHeaderNullableSource, null, 200, string.Empty },
                new object[] { fromHeaderDefaultValueSource, expectedBody, 200, expectedBody },
                new object[] { fromHeaderDefaultValueSource, null, 200, string.Empty },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_ExplicitHeaderParam_SimpleReturn_Data))]
    public async Task MapAction_ExplicitHeaderParam_SimpleReturn(string source, string requestData, int expectedStatusCode, string expectedBody)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        if (requestData is not null)
        {
            httpContext.Request.Headers["headerValue"] = requestData;
        }

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody, expectedStatusCode);
    }

    public static object[][] MapAction_ExplicitServiceParam_SimpleReturn_Data
    {
        get
        {
            var fromServiceRequiredSource = """app.MapPost("/", ([FromServices]TestService svc) => svc.TestServiceMethod());""";
            var fromServiceNullableSource = """app.MapPost("/", ([FromServices]TestService? svc) => svc?.TestServiceMethod() ?? string.Empty);""";
            var fromServiceDefaultValueSource = """
#nullable disable
string postServiceWithDefault([FromServices]TestService svc = null) => svc?.TestServiceMethod() ?? string.Empty;
app.MapPost("/", postServiceWithDefault);
#nullable restore
""";

            var fromServiceEnumerableRequiredSource = """app.MapPost("/", ([FromServices]IEnumerable<TestService>  svc) => svc.FirstOrDefault()?.TestServiceMethod() ?? string.Empty);""";
            var fromServiceEnumerableNullableSource = """app.MapPost("/", ([FromServices]IEnumerable<TestService>? svc) => svc?.FirstOrDefault()?.TestServiceMethod() ?? string.Empty);""";
            var fromServiceEnumerableDefaultValueSource = """
#nullable disable
string postServiceWithDefault([FromServices]IEnumerable<TestService> svc = null) => svc?.FirstOrDefault()?.TestServiceMethod() ?? string.Empty;
app.MapPost("/", postServiceWithDefault);
#nullable restore
""";

            return new[]
            {
                new object[] { fromServiceRequiredSource, true, true },
                new object[] { fromServiceRequiredSource, false, false },
                new object[] { fromServiceNullableSource, true, true },
                new object[] { fromServiceNullableSource, false, true },
                new object[] { fromServiceDefaultValueSource, true, true },
                new object[] { fromServiceDefaultValueSource, false, true },
                new object[] { fromServiceEnumerableRequiredSource, true, true },
                new object[] { fromServiceEnumerableRequiredSource, false, true },
                new object[] { fromServiceEnumerableNullableSource, true, true },
                new object[] { fromServiceEnumerableNullableSource, false, true },
                new object[] { fromServiceEnumerableDefaultValueSource, true, true },
                new object[] { fromServiceEnumerableDefaultValueSource, false, true }
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_ExplicitServiceParam_SimpleReturn_Data))]
    public async Task MapAction_ExplicitServiceParam_SimpleReturn(string source, bool hasService, bool isValid)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        if (hasService)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<TestService>(new TestService());
            var services = serviceCollection.BuildServiceProvider();
            httpContext.RequestServices = services;
        }

        if (isValid)
        {
            await endpoint.RequestDelegate(httpContext);
            await VerifyResponseBodyAsync(httpContext, hasService ? "Produced from service!" : string.Empty);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => endpoint.RequestDelegate(httpContext));
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        }
    }

    [Fact]
    public async Task MapAction_ExplicitServiceParam_SimpleReturn_Snapshot()
    {
        var source = """
app.MapGet("/fromServiceRequired", ([FromServices]TestService svc) => svc.TestServiceMethod());
app.MapGet("/enumerableFromService", ([FromServices]IEnumerable<TestService> svc) => svc?.FirstOrDefault()?.TestServiceMethod() ?? string.Empty);
app.MapGet("/multipleFromService", ([FromServices]TestService? svc, [FromServices]IEnumerable<TestService> svcs) =>
    $"{(svcs?.FirstOrDefault()?.TestServiceMethod() ?? string.Empty)}, {svc?.TestServiceMethod()}");
""";
        var httpContext = CreateHttpContext();
        var expectedBody = "Produced from service!";
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<TestService>(new TestService());
        var services = serviceCollection.BuildServiceProvider();
        var emptyServices = new ServiceCollection().BuildServiceProvider();

        var (_, compilation) = await RunGeneratorAsync(source);

        await VerifyAgainstBaselineUsingFile(compilation);

        var endpoints = GetEndpointsFromCompilation(compilation);

        Assert.Equal(3, endpoints.Length);

        // fromServiceRequired throws on null input
        httpContext.RequestServices = emptyServices;
        await Assert.ThrowsAsync<InvalidOperationException>(() => endpoints[0].RequestDelegate(httpContext));
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);

        // fromServiceRequired accepts a provided input
        httpContext = CreateHttpContext();
        httpContext.RequestServices = services;
        await endpoints[0].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);

        // enumerableFromService
        httpContext = CreateHttpContext();
        httpContext.RequestServices = services;
        await endpoints[1].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);

        // multipleFromService
        httpContext = CreateHttpContext();
        httpContext.RequestServices = services;
        await endpoints[2].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, $"{expectedBody}, {expectedBody}");
    }

    [Fact]
    public async Task MapAction_ExplicitSource_SimpleReturn_Snapshot()
    {
        var source = """
app.MapGet("/fromQuery", ([FromQuery] string queryValue) => queryValue ?? string.Empty);
app.MapGet("/fromHeader", ([FromHeader] string headerValue) => headerValue ?? string.Empty);
app.MapGet("/fromRoute/{routeValue}", ([FromRoute] string routeValue) => routeValue ?? string.Empty);
app.MapGet("/fromRouteRequiredImplicit/{value}", (string value) => value);
app.MapGet("/fromQueryRequiredImplicit", (string value) => value);
""";
        var (_, compilation) = await RunGeneratorAsync(source);

        await VerifyAgainstBaselineUsingFile(compilation);
    }
}
