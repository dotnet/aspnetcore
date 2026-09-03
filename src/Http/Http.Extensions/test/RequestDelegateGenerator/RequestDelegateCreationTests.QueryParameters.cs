// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests
{
    public static IEnumerable<object[]> QueryParamOptionalityData
    {
        get
        {
            return new List<object[]>
                {
                    new object[] { @"(string name) => $""Hello {name}!""", "name", null, true, null},
                    new object[] { @"(string name) => $""Hello {name}!""", "name", "TestName", false, "Hello TestName!" },
                    new object[] { @"(string name = ""DefaultName"") => $""Hello {name}!""", "name", null, false, "Hello DefaultName!" },
                    new object[] { @"(string name = ""DefaultName"") => $""Hello {name}!""", "name", "TestName", false, "Hello TestName!" },
                    new object[] { @"(string? name) => $""Hello {name}!""", "name", null, false, "Hello !" },
                    new object[] { @"(string? name) => $""Hello {name}!""", "name", "TestName", false, "Hello TestName!"},

                    new object[] { @"(int age) => $""Age: {age}""", "age", null, true, null},
                    new object[] { @"(int age) => $""Age: {age}""", "age", "42", false, "Age: 42" },
                    new object[] { @"(int age = 12) => $""Age: {age}""", "age", null, false, "Age: 12" },
                    new object[] { @"(int age = 12) => $""Age: {age}""", "age", "42", false, "Age: 42" },
                    new object[] { @"(int? age) => $""Age: {age}""", "age", null, false, "Age: " },
                    new object[] { @"(int? age) => $""Age: {age}""", "age", "42", false, "Age: 42"},
                };
        }
    }

    [Theory]
    [MemberData(nameof(QueryParamOptionalityData))]
    public async Task RequestDelegateHandlesQueryParamOptionality(string innerSource, string paramName, string queryParam, bool isInvalid, string expectedResponse)
    {
        var source = $"""
string handler{innerSource};
app.MapGet("/", handler);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider();
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        if (queryParam is not null)
        {
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                [paramName] = queryParam
            });
        }

        await endpoint.RequestDelegate(httpContext);

        var logs = TestSink.Writes.ToArray();

        if (isInvalid)
        {
            Assert.Equal(400, httpContext.Response.StatusCode);
            var log = Assert.Single(logs);
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
            var expectedType = paramName == "age" ? "int age" : $"string name";
            var parameterSource = IsGeneratorEnabled ? "route or query string" : "query string";
            Assert.Equal($@"Required parameter ""{expectedType}"" was not provided from {parameterSource}.", log.Message);
        }
        else
        {
            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
            Assert.Equal(expectedResponse, decodedResponseBody);
        }
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
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.QueryString = new QueryString("?p1=Hello&p2=world!");

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ExplicitParsableParameter_StringReturn()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", ([FromQuery] int p1 = 10) => $"{p1}");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "10");
    }

    [Theory]
    [InlineData(null, "Id: 00000000-0000-0000-0000-000000000000")]
    [InlineData("a0e1f2b3-c4d5-4e6f-7a8b-9c0d1e2f3a4b", "Id: a0e1f2b3-c4d5-4e6f-7a8b-9c0d1e2f3a4b")]
    public async Task CanSetGuidParamAsOptionalWithDefaultValue(string queryValue, string expectedResponse)
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", ([FromQuery] Guid id = default) => $"Id: {id}");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        if (queryValue is not null)
        {
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = queryValue
            });
        }

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedResponse);
    }

    [Theory]
    [InlineData(null, "Date: 0001-01-01T00:00:00.0000000")]
    [InlineData("2024-01-15", "Date: 2024-01-15T00:00:00.0000000")]
    public async Task CanSetDateTimeParamAsOptionalWithDefaultValue(string queryValue, string expectedResponse)
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", ([FromQuery] DateTime date = default) => $"Date: {date:O}");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        if (queryValue is not null)
        {
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["date"] = queryValue
            });
        }

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedResponse);
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
