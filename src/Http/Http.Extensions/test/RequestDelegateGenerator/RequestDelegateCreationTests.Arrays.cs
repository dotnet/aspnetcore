// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests
{
    [Fact]
    public async Task MapAction_ExplicitQuery_ComplexTypeArrayParam()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromQuery]ParsableTodo[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=1&p=1");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ExplicitHeader_ComplexTypeArrayParam()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromHeader]ParsableTodo[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Add("p", new StringValues(new string[] { "1", "1" }));

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ExplicitHeader_StringArrayParam()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromHeader]string[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Add("p", new StringValues(new string[] { "1", "1" }));

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ExplicitHeader_NullableStringArrayParam()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromHeader]string?[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Add("p", new StringValues(new string[] { "1", "1" }));

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    public static object[][] TryParsableArrayParameters
    {
        get
        {
            var now = DateTime.Now;

            return new[]
            {
                    // string is not technically "TryParsable", but it's the special case.
                    new object[] { "string[]", new[] { "plain string" }, new[] { "plain string" } },
                    new object[] { "string[]", new[] { "plain string", "" }, new[] { "plain string", "" } },
                    new object[] { "StringValues", new[] { "1", "2", "3" }, new StringValues(new[] { "1", "2", "3" }) },
                    new object[] { "StringValues", new[] { "1", "", "3" }, new StringValues(new[] { "1", "", "3" }) },
                    new object[] { "int[]", new[] { "-1", "2", "3" }, new[] { -1,2,3 } },
                    new object[] { "uint[]", new[] { "1","42","32"}, new[] { 1U, 42U, 32U } },
                    new object[] { "bool[]", new[] { "true", "false" }, new[] { true, false } },
                    new object[] { "short[]", new[] { "-42" }, new[] { (short)-42 } },
                    new object[] { "ushort[]", new[] { "42" }, new[] { (ushort)42 } },
                    new object[] { "long[]", new[] { "-42" }, new[] { -42L } },
                    new object[] { "ulong[]", new[] { "42" }, new[] { 42UL } },
                    new object[] { "IntPtr[]", new[] { "-42" },new[] { new IntPtr(-42) } },
                    new object[] { "char[]", new[] { "A" }, new[] { 'A' } },
                    new object[] { "double[]", new[] { "0.5" },new[] { 0.5 } },
                    new object[] { "float[]", new[] { "0.5" },new[] { 0.5f } },
                    new object[] { "Half[]", new[] { "0.5" }, new[] { (Half)0.5f } },
                    new object[] { "decimal[]", new[] { "0.5" },new[] { 0.5m } },
                    new object[] { "Uri[]", new[] { "https://example.org" }, new[] { new Uri("https://example.org") } },
                    new object[] { "DateTime[]", new[] { now.ToString("o") },new[] { now.ToUniversalTime() } },
                    new object[] { "DateTimeOffset[]", new[] { "1970-01-01T00:00:00.0000000+00:00" },new[] { DateTimeOffset.UnixEpoch } },
                    new object[] { "TimeSpan[]", new[] { "00:00:42" },new[] { TimeSpan.FromSeconds(42) } },
                    new object[] { "Guid[]", new[] { "00000000-0000-0000-0000-000000000000" },new[] { Guid.Empty } },
                    new object[] { "Version[]", new[] { "6.0.0.42" }, new[] { new Version("6.0.0.42") } },
                    new object[] { "BigInteger[]", new[] { "-42" },new[]{ new BigInteger(-42) } },
                    new object[] { "IPAddress[]", new[] { "127.0.0.1" }, new[] { IPAddress.Loopback } },
                    new object[] { "IPEndPoint[]", new[] { "127.0.0.1:80" },new[] { new IPEndPoint(IPAddress.Loopback, 80) } },
                    new object[] { "AddressFamily[]", new[] { "Unix" },new[] { AddressFamily.Unix } },
                    new object[] { "ILOpCode[]", new[] { "Nop" }, new[] { ILOpCode.Nop } },
                    new object[] { "AssemblyFlags[]", new[] { "PublicKey,Retargetable" },new[] { AssemblyFlags.PublicKey | AssemblyFlags.Retargetable } },
                    new object[] { "int?[]", new[] { "42" }, new int?[] { 42 } },
                    new object[] { "MyEnum[]", new[] { "ValueB" },new[] { MyEnum.ValueB } },
                    new object[] { "MyTryParseRecord[]", new[] { "https://example.org" },new[] { new MyTryParseRecord(new Uri("https://example.org")) } },
                    new object[] { "int[]", new string[] {}, Array.Empty<int>() },
                    new object[] { "int?[]", new string[] { "1", "2", null, "4" }, new int?[] { 1,2, null, 4 } },
                    new object[] { "int?[]", new string[] { "1", "2", "", "4" }, new int?[] { 1,2, null, 4 } },
                    new object[] { "MyTryParseRecord?[]?", new[] { "" }, new MyTryParseRecord[] { null } },
                };
        }
    }

    [Theory]
    [MemberData(nameof(TryParsableArrayParameters))]
    public async Task RequestDelegateHandlesArraysFromQueryString(string typeName, string[] queryValues, object expectedParameterValue)
    {
        var (results, compilation) = await RunGeneratorAsync($$"""
app.MapGet("/hello", (HttpContext context, {{typeName}} tryParsable) => {
    context.Items["tryParsable"] = tryParsable;
    });
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["tryParsable"] = queryValues
        });

        await endpoint.RequestDelegate(httpContext);

        Assert.NotEmpty(httpContext.Items);
        Assert.Equal(expectedParameterValue, httpContext.Items["tryParsable"]);
    }

    [Fact]
    public async Task MapAction_ImplicitQuery_ComplexTypeArrayParam()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (ParsableTodo[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=1&p=1");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ExplicitQuery_StringArrayParam()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromQuery]string[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=Item1&p=Item2");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ImplicitQuery_StringArrayParam()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (string[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=Item1&p=Item2");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ImplicitQuery_NullableStringArrayParam_QueryNotPresent()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (string?[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "0");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ImplicitQuery_NullableStringArrayParam_EmptyQueryValues()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (string?[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=&p=");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ExplicitQuery_NullableStringArrayParam()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromQuery]string?[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=Item1&p=Item2");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ImplicitQuery_NullableStringArrayParam()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (string?[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=Item1&p=Item2");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "2");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapPost_WithArrayQueryString_ShouldFail()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapPost("/hello", (string[] p) => p.Length);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapPost", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p=Item1&p=Item2");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "", expectedStatusCode: 400);
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapPost_WithArrayQueryString_AndBody_ShouldUseBody()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapPost("/hello", (string[] p) => p[0]);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapPost", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.Request.Headers["Content-Type"] = "application/json";
        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new string[] { "ValueFromBody" });
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Request.QueryString = new QueryString("?p=ValueFromQueryString");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "ValueFromBody");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapMethods_Post_WithArrayQueryString_AndBody_ShouldUseBody()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapMethods("/hello", new [] { "POST" }, (string[] p) => p[0]);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapMethods", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.Request.Headers["Content-Type"] = "application/json";
        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new string[] { "ValueFromBody" });
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Request.QueryString = new QueryString("?p=ValueFromQueryString");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "ValueFromBody");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapMethods_Get_WithArrayQueryString_AndBody_ShouldUseQueryString()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapMethods("/hello", new [] { "GET" }, (string[] p) => p[0]);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapMethods", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.Request.Headers["Content-Type"] = "application/json";
        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new string[] { "ValueFromBody" });
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Request.QueryString = new QueryString("?p=ValueFromQueryString");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "ValueFromQueryString");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapMethods_PostAndGet_WithArrayQueryString_AndBody_ShouldUseQueryString()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapMethods("/hello", new [] { "POST", "GET" }, (string[] p) => p[0]);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapMethods", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.Request.Headers["Content-Type"] = "application/json";
        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new string[] { "ValueFromBody" });
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Request.QueryString = new QueryString("?p=ValueFromQueryString");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "ValueFromQueryString");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapMethods_PostAndPut_WithArrayQueryString_AndBody_ShouldUseBody()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapMethods("/hello", new [] { "POST", "PUT" }, (string[] p) => p[0]);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapMethods", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.Request.Headers["Content-Type"] = "application/json";
        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new string[] { "ValueFromBody" });
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Request.QueryString = new QueryString("?p=ValueFromQueryString");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "ValueFromBody");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    public async Task RequestDelegateHandlesArraysFromExplicitQueryStringSource()
    {
        var source = """
app.MapPost("/", (HttpContext context,
    [FromHeader(Name = "Custom")] int[] headerValues,
    [FromQuery(Name = "a")] int[] queryValues,
    [FromForm(Name = "form")] int[] formValues) =>
{
    context.Items["headers"] = headerValues;
    context.Items["query"] = queryValues;
    context.Items["form"] = formValues;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["a"] = new(new[] { "1", "2", "3" })
        });

        httpContext.Request.Headers["Custom"] = new(new[] { "4", "5", "6" });

        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["form"] = new(new[] { "7", "8", "9" })
        });

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(new[] { 1, 2, 3 }, (int[])httpContext.Items["query"]!);
        Assert.Equal(new[] { 4, 5, 6 }, (int[])httpContext.Items["headers"]!);
        Assert.Equal(new[] { 7, 8, 9 }, (int[])httpContext.Items["form"]!);
    }
}
