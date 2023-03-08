// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests
{
    public static object[][] MapAction_ExplicitBodyParam_ComplexReturn_Data
    {
        get
        {
            var expectedBody = """{"id":0,"name":"Test Item","isComplete":false}""";
            var todo = new Todo()
            {
                Id = 0,
                Name = "Test Item",
                IsComplete = false
            };
            var withFilter = """
.AddEndpointFilter((c, n) => n(c));
""";
            var fromBodyRequiredSource = """app.MapPost("/", ([FromBody] Todo todo) => TypedResults.Ok(todo));""";
            var fromBodyEmptyBodyBehaviorSource = """app.MapPost("/", ([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Todo todo) => TypedResults.Ok(todo));""";
            var fromBodyAllowEmptySource = """app.MapPost("/", ([CustomFromBody(AllowEmpty = true)] Todo todo) => TypedResults.Ok(todo));""";
            var fromBodyNullableSource = """app.MapPost("/", ([FromBody] Todo? todo) => TypedResults.Ok(todo));""";
            var fromBodyDefaultValueSource = """
#nullable disable
IResult postTodoWithDefault([FromBody] Todo todo = null) => TypedResults.Ok(todo);
app.MapPost("/", postTodoWithDefault);
#nullable restore
""";
            var fromBodyRequiredWithFilterSource = $"""app.MapPost("/", ([FromBody] Todo todo) => TypedResults.Ok(todo)){withFilter}""";
            var fromBodyEmptyBehaviorWithFilterSource = $"""app.MapPost("/", ([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Todo todo) => TypedResults.Ok(todo)){withFilter}""";
            var fromBodyAllowEmptyWithFilterSource = $"""app.MapPost("/", ([CustomFromBody(AllowEmpty = true)] Todo todo) => TypedResults.Ok(todo)){withFilter}""";
            var fromBodyNullableWithFilterSource = $"""app.MapPost("/", ([FromBody] Todo?  todo) => TypedResults.Ok(todo)){withFilter}""";
            var fromBodyDefaultValueWithFilterSource = $"""
#nullable disable
IResult postTodoWithDefault([FromBody] Todo todo = null) => TypedResults.Ok(todo);
app.MapPost("/", postTodoWithDefault){withFilter}
#nullable restore
""";

            return new[]
            {
                new object[] { fromBodyRequiredSource, todo, 200, expectedBody },
                new object[] { fromBodyRequiredSource, null, 400, string.Empty },
                new object[] { fromBodyEmptyBodyBehaviorSource, todo, 200, expectedBody },
                new object[] { fromBodyEmptyBodyBehaviorSource, null, 200, string.Empty },
                new object[] { fromBodyAllowEmptySource, todo, 200, expectedBody },
                new object[] { fromBodyAllowEmptySource, null, 200, string.Empty },
                new object[] { fromBodyNullableSource, todo, 200, expectedBody },
                new object[] { fromBodyNullableSource, null, 200, string.Empty },
                new object[] { fromBodyDefaultValueSource, todo, 200, expectedBody },
                new object[] { fromBodyDefaultValueSource, null, 200, string.Empty },
                new object[] { fromBodyRequiredWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyRequiredWithFilterSource, null, 400, string.Empty },
                new object[] { fromBodyEmptyBehaviorWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyEmptyBehaviorWithFilterSource, null, 200, string.Empty },
                new object[] { fromBodyAllowEmptyWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyAllowEmptyWithFilterSource, null, 200, string.Empty },
                new object[] { fromBodyNullableWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyNullableWithFilterSource, null, 200, string.Empty },
                new object[] { fromBodyDefaultValueWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyDefaultValueSource, null, 200, string.Empty },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_ExplicitBodyParam_ComplexReturn_Data))]
    public async Task MapAction_ExplicitBodyParam_ComplexReturn(string source, Todo requestData, int expectedStatusCode, string expectedBody)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(requestData is not null));
        httpContext.Request.Headers["Content-Type"] = "application/json";

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(requestData);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody, expectedStatusCode);
    }

    [Fact]
    public async Task MapAction_ExplicitBodyParam_ComplexReturn_Returns400ForEmptyBody()
    {
        var source = """app.MapPost("/", ([FromBody] Todo todo) => TypedResults.Ok(todo));""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(false));
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "0";

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, string.Empty, expectedStatusCode: 400);
    }

    [Fact]
    public async Task MapAction_ExplicitBodyParam_ComplexReturn_Snapshot()
    {
        var expectedBody = """{"id":0,"name":"Test Item","isComplete":false}""";
        var todo = new Todo()
        {
            Id = 0,
            Name = "Test Item",
            IsComplete = false
        };
        var source = $"""
app.MapPost("/fromBodyRequired", ([FromBody] Todo todo) => TypedResults.Ok(todo));
#pragma warning disable CS8622
app.MapPost("/fromBodyOptional", ([FromBody] Todo? todo) => TypedResults.Ok(todo));
#pragma warning restore CS8622
""";
        var (_, compilation) = await RunGeneratorAsync(source);

        await VerifyAgainstBaselineUsingFile(compilation);

        var endpoints = GetEndpointsFromCompilation(compilation);

        Assert.Equal(2, endpoints.Length);

        // formBodyRequired accepts a provided input
        var httpContext = CreateHttpContextWithBody(todo);
        await endpoints[0].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);

        // formBodyRequired throws on null input
        httpContext = CreateHttpContextWithBody(null);
        await endpoints[0].RequestDelegate(httpContext);
        Assert.Equal(400, httpContext.Response.StatusCode);

        // formBodyOptional accepts a provided input
        httpContext = CreateHttpContextWithBody(todo);
        await endpoints[1].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);

        // formBodyOptional accepts a null input
        httpContext = CreateHttpContextWithBody(null);
        await endpoints[1].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, string.Empty);
    }
}
