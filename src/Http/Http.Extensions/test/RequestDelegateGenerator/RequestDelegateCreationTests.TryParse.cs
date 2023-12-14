// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests
{
    public static object[][] TryParsableParameters
    {
        get
        {
            var now = DateTime.Now;

            return new[]
            {
                // string is not technically "TryParsable", but it's the special case.
                new object[] { "string", "plain string", "plain string" },
                new object[] { "int", "-42", -42 },
                new object[] { "uint", "42", 42U },
                new object[] { "bool", "true", true },
                new object[] { "short", "-42", (short)-42 },
                new object[] { "ushort", "42", (ushort)42 },
                new object[] { "long", "-42", -42L },
                new object[] { "ulong", "42", 42UL },
                new object[] { "IntPtr", "-42", new IntPtr(-42) },
                new object[] { "char", "A", 'A' },
                new object[] { "double", "0.5", 0.5 },
                new object[] { "float", "0.5", 0.5f },
                new object[] { "Half", "0.5", (Half)0.5f },
                new object[] { "decimal", "0.5", 0.5m },
                new object[] { "Uri", "https://example.org", new Uri("https://example.org") },
                new object[] { "Uri?", "https://example.org", new Uri("https://example.org") },
                new object[] { "Uri?", null, null },
                new object[] { "DateTime", now.ToString("o"), now.ToUniversalTime() },
                new object[] { "DateTimeOffset", "1970-01-01T00:00:00.0000000+00:00", DateTimeOffset.UnixEpoch },
                new object[] { "TimeSpan", "00:00:42", TimeSpan.FromSeconds(42) },
                new object[] { "TimeOnly", "4:34 PM   ", new TimeOnly(16, 34) },
                new object[] { "DateOnly", "9/20/2021   ", new DateOnly(2021, 9, 20) },
                new object[] { "Guid", "00000000-0000-0000-0000-000000000000", Guid.Empty },
                new object[] { "Version", "6.0.0.42", new Version("6.0.0.42") },
                new object[] { "BigInteger", "-42", new BigInteger(-42) },
                new object[] { "IPAddress", "127.0.0.1", IPAddress.Loopback },
                new object[] { "IPEndPoint", "127.0.0.1:80", new IPEndPoint(IPAddress.Loopback, 80) },
                new object[] { "AddressFamily", "Unix", AddressFamily.Unix },
                new object[] { "ILOpCode", "Nop", ILOpCode.Nop },
                new object[] { "AssemblyFlags", "PublicKey,Retargetable", AssemblyFlags.PublicKey | AssemblyFlags.Retargetable },
                new object[] { "MyEnum", "ValueB", MyEnum.ValueB },
                new object[] { "MyTryParseRecord", "https://example.org", new MyTryParseRecord(new Uri("https://example.org")) },
                new object[] { "int?", "42", 42 },
                new object[] { "int?", null, null }
            };
        }
    }

    [Theory]
    [MemberData(nameof(TryParsableParameters))]
    public async Task MapAction_SingleParsable_StringReturn(string typeName, string queryStringInput, object expectedParameterValue)
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapGet("/hello", (HttpContext context, [FromQuery]{{typeName}} p) =>
{
    context.Items["tryParsable"] = p;
});
""");
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        if (queryStringInput != null)
        {
            httpContext.Request.QueryString = new QueryString($"?p={UrlEncoder.Default.Encode(queryStringInput)}");
        }

        await endpoint.RequestDelegate(httpContext);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedParameterValue, httpContext.Items["tryParsable"]);
    }

    public static object[][] MapAction_DateTime_Data
    {
        get
        {
            return new[]
            {
                new object[] { "9/20/2021 4:18:44 PM", "Time: 2021-09-20T16:18:44.0000000, Kind: Unspecified" },
                new object[] { "2021-09-20 4:18:44", "Time: 2021-09-20T04:18:44.0000000, Kind: Unspecified" },
                new object[] { "   9/20/2021    4:18:44 PM  ", "Time: 2021-09-20T16:18:44.0000000, Kind: Unspecified" },
                new object[] { "2021-09-20T16:28:02.000-07:00", "Time: 2021-09-20T23:28:02.0000000Z, Kind: Utc" },
                new object[] { "  2021-09-20T 16:28:02.000-07:00  ", "Time: 2021-09-20T23:28:02.0000000Z, Kind: Utc" },
                new object[] { "2021-09-20T23:30:02.000+00:00", "Time: 2021-09-20T23:30:02.0000000Z, Kind: Utc" },
                new object[] { "     2021-09-20T23:30: 02.000+00:00 ", "Time: 2021-09-20T23:30:02.0000000Z, Kind: Utc" },
                new object[] { "2021-09-20 16:48:02-07:00", "Time: 2021-09-20T23:48:02.0000000Z, Kind: Utc" },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_DateTime_Data))]
    public async Task MapAction_DateTime_StringReturn(string queryStringInput, string expectedBody)
    {
        var source = """
app.MapGet("/", (HttpContext context, [FromQuery] DateTime time)
            => $"Time: {time.ToString("O", System.Globalization.CultureInfo.InvariantCulture)}, Kind: {time.Kind}");
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        httpContext.Request.QueryString = new QueryString($"?time={UrlEncoder.Default.Encode(queryStringInput)}");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    public static object[][] MapAction_DateTimeOffset_Data
    {
        get
        {
            return new[]
            {
                new object[] { "09/20/2021 16:35:12 +00:00", "Time: 2021-09-20T16:35:12.0000000+00:00, Offset: 00:00:00" },
                new object[] { "09/20/2021 11:35:12 +07:00", "Time: 2021-09-20T11:35:12.0000000+07:00, Offset: 07:00:00" },
                new object[] { "09/20/2021 16:35:12", "Time: 2021-09-20T16:35:12.0000000+00:00, Offset: 00:00:00" },
                new object[] { " 09/20/2021 16:35:12 ", "Time: 2021-09-20T16:35:12.0000000+00:00, Offset: 00:00:00" },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_DateTimeOffset_Data))]
    public async Task MapAction_DateTimeOffset_StringReturn(string queryStringInput, string expectedBody)
    {
        var source = """
app.MapGet("/", (HttpContext context, [FromQuery] DateTimeOffset time)
            => $"Time: {time.ToString("O", System.Globalization.CultureInfo.InvariantCulture)}, Offset: {time.Offset}");
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        httpContext.Request.QueryString = new QueryString($"?time={UrlEncoder.Default.Encode(queryStringInput)}");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Theory]
    [InlineData("PrecedenceCheckTodoWithoutFormat", "24")]
    [InlineData("PrecedenceCheckTodo", "42")]
    public async Task MapAction_TryParsePrecedenceCheck(string parameterType, string result)
    {
        var (results, compilation) = await RunGeneratorAsync($$"""
app.MapGet("/hello", ([FromQuery]{{parameterType}} p) => p.MagicValue);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, (endpointModel) =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            var p = Assert.Single(endpointModel.Parameters);
            Assert.Equal(EndpointParameterSource.Query, p.Source);
            Assert.Equal("p", p.SymbolName);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=1");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, result);
    }

    [Fact]
    public async Task MapAction_SingleComplexTypeParam_StringReturn()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromQuery]TryParseTodo p) => p.Name!);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            var p = Assert.Single(endpointModel.Parameters);
            Assert.Equal(EndpointParameterSource.Query, p.Source);
            Assert.Equal("p", p.SymbolName);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=1");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Knit kitten mittens.");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ExplicitIParsable()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromQuery]TodoWithExplicitIParsable p, HttpContext context) => context.Items["p"] = p);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=1");

        await endpoint.RequestDelegate(httpContext);
        var p = httpContext.Items["p"];

        Assert.NotNull(p);
    }

    [Fact]
    public async Task MapAction_SingleEnumParam_StringReturn()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromQuery]TodoStatus p) => p.ToString());
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            var p = Assert.Single(endpointModel.Parameters);
            Assert.Equal(EndpointParameterSource.Query, p.Source);
            Assert.Equal("p", p.SymbolName);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=Done");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Done");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Theory]
    [MemberData(nameof(TryParsableParameters))]
    public async Task RequestDelegatePopulatesUnattributedTryParsableParametersFromRouteValue(string typeName, string routeValue, object expectedParameterValue)
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapGet("/hello/{tryParsable}", (HttpContext context, {{typeName}} tryParsable) =>
{
    context.Items["tryParsable"] = tryParsable;
});
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["tryParsable"] = routeValue;
        var requestDelegate = endpoint.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(expectedParameterValue, httpContext.Items["tryParsable"]);
    }

    [Theory]
    [InlineData("void TestAction(HttpContext httpContext, [FromRoute] MyBindAsyncRecord myBindAsyncRecord) { }")]
    [InlineData("void TestAction(HttpContext httpContext, [FromQuery] MyBindAsyncRecord myBindAsyncRecord) { }")]
    public async Task RequestDelegateUsesTryParseOverBindAsyncGivenExplicitAttribute(string source)
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
{{source}}
app.MapGet("/{myBindAsyncRecord}", TestAction);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["myBindAsyncRecord"] = "foo";
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["myBindAsyncRecord"] = "foo"
        });

        await Assert.ThrowsAsync<NotImplementedException>(async () => await endpoint.RequestDelegate(httpContext));
    }
}
