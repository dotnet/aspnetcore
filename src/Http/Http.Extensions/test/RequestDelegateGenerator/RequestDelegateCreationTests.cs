// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
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

    public static object[][] CanApplyFiltersOnHandlerWithVariousArguments_Data
    {
        get
        {
            var tooManyArguments = """
string HelloName([FromQuery] int? one, [FromQuery] string? two, [FromQuery] int? three, [FromQuery] string? four,
    [FromQuery] int? five, [FromQuery] bool? six, [FromQuery] string? seven, [FromQuery] string? eight,
    [FromQuery] int? nine, [FromQuery] string? ten, [FromQuery] int? eleven) =>
    "Too many arguments";
""";
            var noArguments = """
string HelloName() => "No arguments";
""";
            var justRightArguments = """
string HelloName([FromQuery] int? one, [FromQuery] string? two, [FromQuery] int? three, [FromQuery] string? four,
    [FromQuery] int? five, [FromQuery] bool? six, [FromQuery] string? seven) =>
    "Just right arguments";
""";
            return new object[][]
            {
                new [] { tooManyArguments, "True, 11, Too many arguments" },
                new [] { noArguments, "True, 0, No arguments" },
                new [] { justRightArguments, "False, 7, Just right arguments" },
            };
        }
    }

    [Theory]
    [MemberData(nameof(CanApplyFiltersOnHandlerWithVariousArguments_Data))]
    public async Task CanApplyFiltersOnHandlerWithVariousArguments(string handlerMethod, string expectedBody)
    {
        var source = $$"""
{{handlerMethod}}
app.MapGet("/", HelloName)
    .AddEndpointFilter(async (c, n) =>
    {
        var result = await n(c);
        return $"{(c is DefaultEndpointFilterInvocationContext)}, {c.Arguments.Count}, {result}";
    });
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);

        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Fact]
    public async Task MapAction_InferredTryParse_NonOptional_Provided()
    {
        var source = """
app.MapGet("/", (HttpContext httpContext, int id) =>
{
    httpContext.Items["id"] = id;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["id"] = "42",
        });

        httpContext.Request.Headers.Referer = "https://example.org";
        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(42, httpContext.Items["id"]);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    public static object[][] BindAsyncUriTypesAndOptionalitySupport = new object[][]
    {
        new object[] { "MyBindAsyncRecord", false },
        new object[] { "MyBindAsyncStruct", true },
        new object[] { "MyNullableBindAsyncStruct", false },
        new object[] { "MyBothBindAsyncStruct", true },
        new object[] { "MySimpleBindAsyncRecord", false, },
        new object[] { "MySimpleBindAsyncStruct", true },
        new object[] { "BindAsyncFromImplicitStaticAbstractInterface", false },
        new object[] { "InheritBindAsync", false },
        new object[] { "BindAsyncFromExplicitStaticAbstractInterface", false },
        // TODO: Fix this
        //new object[] { "MyBindAsyncFromInterfaceRecord", false },
    };

    public static IEnumerable<object[]> BindAsyncUriTypes =>
        BindAsyncUriTypesAndOptionalitySupport.Select(x => new[] { x[0] });

    [Theory]
    [MemberData(nameof(BindAsyncUriTypes))]
    public async Task MapAction_BindAsync_Optional_Provided(string bindAsyncType)
    {
        var source = $$"""
app.MapGet("/", (HttpContext httpContext, {{bindAsyncType}}? myBindAsyncParam) =>
{
    httpContext.Items["uri"] = myBindAsyncParam?.Uri;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Referer = "https://example.org";
        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(new Uri("https://example.org"), httpContext.Items["uri"]);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(BindAsyncUriTypes))]
    public async Task MapAction_BindAsync_NonOptional_Provided(string bindAsyncType)
    {
        var source = $$"""
app.MapGet("/", (HttpContext httpContext, {{bindAsyncType}} myBindAsyncParam) =>
{
    httpContext.Items["uri"] = myBindAsyncParam.Uri;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Referer = "https://example.org";
        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(new Uri("https://example.org"), httpContext.Items["uri"]);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(BindAsyncUriTypesAndOptionalitySupport))]
    public async Task MapAction_BindAsync_Optional_NotProvided(string bindAsyncType, bool expectException)
    {
        var source = $$"""
app.MapGet("/", (HttpContext httpContext, {{bindAsyncType}}? myBindAsyncParam) =>
{
    httpContext.Items["uri"] = myBindAsyncParam?.Uri;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        if (expectException)
        {
            // These types simply don't support optional parameters since they cannot return null.
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));
            Assert.Equal("The request is missing the required Referer header.", ex.Message);
        }
        else
        {
            await endpoint.RequestDelegate(httpContext);

            Assert.Null(httpContext.Items["uri"]);
            Assert.Equal(200, httpContext.Response.StatusCode);
        }
    }

    [Theory]
    [MemberData(nameof(BindAsyncUriTypesAndOptionalitySupport))]
    public async Task MapAction_BindAsync_NonOptional_NotProvided(string bindAsyncType, bool expectException)
    {
        var source = $$"""
app.MapGet("/", (HttpContext httpContext, {{bindAsyncType}} myBindAsyncParam) =>
{
    httpContext.Items["uri"] = myBindAsyncParam.Uri;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        if (expectException)
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));
            Assert.Equal("The request is missing the required Referer header.", ex.Message);
        }
        else
        {
            await endpoint.RequestDelegate(httpContext);

            Assert.Null(httpContext.Items["uri"]);
            Assert.Equal(400, httpContext.Response.StatusCode);
        }
    }

    [Fact]
    public async Task MapAction_BindAsync_Snapshot()
    {
        var source = new StringBuilder();

        var i = 0;
        while (i < BindAsyncUriTypesAndOptionalitySupport.Length * 2)
        {
            var bindAsyncType = BindAsyncUriTypesAndOptionalitySupport[i / 2][0];
            source.AppendLine(CultureInfo.InvariantCulture, $$"""app.MapGet("/{{i}}", (HttpContext httpContext, {{bindAsyncType}} myBindAsyncParam) => "Hello world! {{i}}");""");
            i++;
            source.AppendLine(CultureInfo.InvariantCulture, $$"""app.MapGet("/{{i}}", ({{bindAsyncType}}? myBindAsyncParam) => "Hello world! {{i}}");""");
            i++;
        }

        var (_, compilation) = await RunGeneratorAsync(source.ToString());

        await VerifyAgainstBaselineUsingFile(compilation);

        var endpoints = GetEndpointsFromCompilation(compilation);
        Assert.Equal(BindAsyncUriTypesAndOptionalitySupport.Length * 2, endpoints.Length);

        for (i = 0; i < BindAsyncUriTypesAndOptionalitySupport.Length * 2; i++)
        {
            var httpContext = CreateHttpContext();
            // Set a referrer header so BindAsync always succeeds and the route handler is always called optional or not.
            httpContext.Request.Headers.Referer = "https://example.org";

            await endpoints[i].RequestDelegate(httpContext);
            await VerifyResponseBodyAsync(httpContext, $"Hello world! {i}");
        }
    }

    [Fact]
    public async Task MapAction_BindAsync_ExceptionsAreUncaught()
    {
        var source = """
app.MapGet("/", (HttpContext httpContext, MyBindAsyncTypeThatThrows myBindAsyncParam) => { });
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Referer = "https://example.org";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => endpoint.RequestDelegate(httpContext));
        Assert.Equal("BindAsync failed", ex.Message);
    }
}
