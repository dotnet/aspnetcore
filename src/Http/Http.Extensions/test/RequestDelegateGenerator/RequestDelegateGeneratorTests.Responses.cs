// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateGeneratorTests
{
    [Theory]
    [InlineData(@"app.MapGet(""/hello"", () => ""Hello world!"");", "MapGet", "Hello world!")]
    [InlineData(@"app.MapPost(""/hello"", () => ""Hello world!"");", "MapPost", "Hello world!")]
    [InlineData(@"app.MapDelete(""/hello"", () => ""Hello world!"");", "MapDelete", "Hello world!")]
    [InlineData(@"app.MapPut(""/hello"", () => ""Hello world!"");", "MapPut", "Hello world!")]
    [InlineData(@"app.MapGet(pattern: ""/hello"", handler: () => ""Hello world!"");", "MapGet", "Hello world!")]
    [InlineData(@"app.MapPost(handler: () => ""Hello world!"", pattern: ""/hello"");", "MapPost", "Hello world!")]
    [InlineData(@"app.MapDelete(pattern: ""/hello"", handler: () => ""Hello world!"");", "MapDelete", "Hello world!")]
    [InlineData(@"app.MapPut(handler: () => ""Hello world!"", pattern: ""/hello"");", "MapPut", "Hello world!")]
    public async Task MapAction_NoParam_StringReturn(string source, string httpMethod, string expectedBody)
    {
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, (endpointModel) =>
        {
            Assert.Equal("/hello", endpointModel.RoutePattern);
            Assert.Equal(httpMethod, endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Fact]
    public async Task MapAction_NoParam_StringReturn_WithFilter()
    {
        var source = """
app.MapGet("/hello", () => "Hello world!")
    .AddEndpointFilter(async (context, next) => {
        var result = await next(context);
        return $"Filtered: {result}";
    });
""";
        var expectedBody = "Filtered: Hello world!";
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        await VerifyAgainstBaselineUsingFile(compilation);
        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("/hello", endpointModel.RoutePattern);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => 123456);", "123456")]
    [InlineData(@"app.MapGet(""/"", () => true);", "true")]
    [InlineData(@"app.MapGet(""/"", () => new DateTime(2023, 1, 1));", @"""2023-01-01T00:00:00""")]
    public async Task MapAction_NoParam_AnyReturn(string source, string expectedBody)
    {
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("/", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    public static IEnumerable<object[]> MapAction_NoParam_ComplexReturn_Data => new List<object[]>()
    {
        new object[] { """app.MapGet("/", () => new Todo() { Name = "Test Item"});""" },
        new object[] { """
object GetTodo() => new Todo() { Name = "Test Item"};
app.MapGet("/", GetTodo);
"""},
        new object[] { """app.MapGet("/", () => TypedResults.Ok(new Todo() { Name = "Test Item"}));""" }
    };

    [Theory]
    [MemberData(nameof(MapAction_NoParam_ComplexReturn_Data))]
    public async Task MapAction_NoParam_ComplexReturn(string source)
    {
        var expectedBody = """{"id":0,"name":"Test Item","isComplete":false}""";
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("/", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    public static IEnumerable<object[]>  MapAction_NoParam_TaskOfTReturn_Data => new List<object[]>()
    {
        new object[] { @"app.MapGet(""/"", () => Task.FromResult(""Hello world!""));", "Hello world!" },
        new object[] { @"app.MapGet(""/"", () => Task.FromResult(new Todo() { Name = ""Test Item"" }));", """{"id":0,"name":"Test Item","isComplete":false}""" },
        new object[] { @"app.MapGet(""/"", () => Task.FromResult(TypedResults.Ok(new Todo() { Name = ""Test Item"" })));", """{"id":0,"name":"Test Item","isComplete":false}""" }
    };

    [Theory]
    [MemberData(nameof(MapAction_NoParam_TaskOfTReturn_Data))]
    public async Task MapAction_NoParam_TaskOfTReturn(string source, string expectedBody)
    {
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("/", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            Assert.True(endpointModel.Response.IsAwaitable);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    public static IEnumerable<object[]> MapAction_NoParam_ValueTaskOfTReturn_Data => new List<object[]>()
    {
        new object[] { @"app.MapGet(""/"", () => ValueTask.FromResult(""Hello world!""));", "Hello world!" },
        new object[] { @"app.MapGet(""/"", () => ValueTask.FromResult(new Todo() { Name = ""Test Item""}));", """{"id":0,"name":"Test Item","isComplete":false}""" },
        new object[] { @"app.MapGet(""/"", () => ValueTask.FromResult(TypedResults.Ok(new Todo() { Name = ""Test Item""})));", """{"id":0,"name":"Test Item","isComplete":false}""" }
    };

    [Theory]
    [MemberData(nameof(MapAction_NoParam_ValueTaskOfTReturn_Data))]
    public async Task MapAction_NoParam_ValueTaskOfTReturn(string source, string expectedBody)
    {
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("/", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            Assert.True(endpointModel.Response.IsAwaitable);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    public static IEnumerable<object[]> MapAction_NoParam_TaskLikeOfObjectReturn_Data => new List<object[]>()
    {
        new object[] { @"app.MapGet(""/"", () => new ValueTask<object>(""Hello world!""));", "Hello world!" },
        new object[] { @"app.MapGet(""/"", () => Task<object>.FromResult(""Hello world!""));", "Hello world!" },
        new object[] { @"app.MapGet(""/"", () => new ValueTask<object>(new Todo() { Name = ""Test Item""}));", """{"id":0,"name":"Test Item","isComplete":false}""" },
        new object[] { @"app.MapGet(""/"", () => Task<object>.FromResult(new Todo() { Name = ""Test Item""}));", """{"id":0,"name":"Test Item","isComplete":false}""" },
        new object[] { @"app.MapGet(""/"", () => new ValueTask<object>(TypedResults.Ok(new Todo() { Name = ""Test Item""})));", """{"id":0,"name":"Test Item","isComplete":false}""" },
        new object[] { @"app.MapGet(""/"", () => Task<object>.FromResult(TypedResults.Ok(new Todo() { Name = ""Test Item""})));", """{"id":0,"name":"Test Item","isComplete":false}""" }
    };

    [Theory]
    [MemberData(nameof(MapAction_NoParam_TaskLikeOfObjectReturn_Data))]
    public async Task MapAction_NoParam_TaskLikeOfObjectReturn(string source, string expectedBody)
    {
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("/", endpointModel.RoutePattern);
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            Assert.True(endpointModel.Response.IsAwaitable);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }
}
