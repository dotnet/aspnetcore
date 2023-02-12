// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public class RequestDelegateGeneratorTests : RequestDelegateGeneratorTestBase
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

        var endpointModel = GetStaticEndpoint(result, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Equal("/hello", endpointModel.RoutePattern);
        Assert.Equal(httpMethod, endpointModel.HttpMethod);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

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

        var endpointModel = GetStaticEndpoint(results, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Equal("/hello", endpointModel.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);
        var p = Assert.Single(endpointModel.Parameters);
        Assert.Equal(EndpointParameterSource.SpecialType, p.Source);
        Assert.Equal("p", p.Name);

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

        var endpointModel = GetStaticEndpoint(results, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

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

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
    }

    [Fact]
    public async Task MapGet_WithRequestDelegate_DoesNotGenerateSources()
    {
        var (results, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (HttpContext context) => Task.CompletedTask);
""");

        Assert.Empty(GetStaticEndpoints(results, GeneratorSteps.EndpointModelStep));

        var endpoint = GetEndpointFromCompilation(compilation, expectSourceKey: false);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "");
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

        var endpointModel = GetStaticEndpoint(result, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Equal("/hello", endpointModel.RoutePattern);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
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

        await VerifyAgainstBaselineUsingFile(compilation);

        var endpointModel = GetStaticEndpoint(result, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Equal("/hello", endpointModel.RoutePattern);

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

        var endpointModel = GetStaticEndpoint(result, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Equal("/", endpointModel.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => new Todo() { Name = ""Test Item""});")]
    [InlineData("""
object GetTodo() => new Todo() { Name = "Test Item"};
app.MapGet("/", GetTodo);
""")]
    [InlineData(@"app.MapGet(""/"", () => TypedResults.Ok(new Todo() { Name = ""Test Item""}));")]
    public async Task MapAction_NoParam_ComplexReturn(string source)
    {
        var expectedBody = """{"id":0,"name":"Test Item","isComplete":false}""";
        var (result, compilation) = await RunGeneratorAsync(source);

        var endpointModel = GetStaticEndpoint(result, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Equal("/", endpointModel.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => Console.WriteLine(""Returns void""));", null)]
    [InlineData(@"app.MapGet(""/"", () => TypedResults.Ok(""Alright!""));", null)]
    [InlineData(@"app.MapGet(""/"", () => Results.NotFound(""Oops!""));", null)]
    [InlineData(@"app.MapGet(""/"", () => Task.FromResult(new Todo() { Name = ""Test Item""}));", "application/json")]
    [InlineData(@"app.MapGet(""/"", () => ""Hello world!"");", "text/plain")]
    public async Task MapAction_ProducesCorrectContentType(string source, string expectedContentType)
    {
        var (result, compilation) = await RunGeneratorAsync(source);

        var endpointModel = GetStaticEndpoint(result, GeneratorSteps.EndpointModelStep);

        Assert.Equal("/", endpointModel.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);
        Assert.Equal(expectedContentType, endpointModel.Response.ContentType);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => Task.FromResult(""Hello world!""));", "Hello world!")]
    [InlineData(@"app.MapGet(""/"", () => Task.FromResult(new Todo() { Name = ""Test Item""}));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    [InlineData(@"app.MapGet(""/"", () => Task.FromResult(TypedResults.Ok(new Todo() { Name = ""Test Item""})));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    public async Task MapAction_NoParam_TaskOfTReturn(string source, string expectedBody)
    {
        var (result, compilation) = await RunGeneratorAsync(source);

        var endpointModel = GetStaticEndpoint(result, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Equal("/", endpointModel.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);
        Assert.True(endpointModel.Response.IsAwaitable);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => ValueTask.FromResult(""Hello world!""));", "Hello world!")]
    [InlineData(@"app.MapGet(""/"", () => ValueTask.FromResult(new Todo() { Name = ""Test Item""}));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    [InlineData(@"app.MapGet(""/"", () => ValueTask.FromResult(TypedResults.Ok(new Todo() { Name = ""Test Item""})));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    public async Task MapAction_NoParam_ValueTaskOfTReturn(string source, string expectedBody)
    {
        var (result, compilation) = await RunGeneratorAsync(source);

        var endpointModel = GetStaticEndpoint(result, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Equal("/", endpointModel.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);
        Assert.True(endpointModel.Response.IsAwaitable);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => new ValueTask<object>(""Hello world!""));", "Hello world!")]
    [InlineData(@"app.MapGet(""/"", () => Task<object>.FromResult(""Hello world!""));", "Hello world!")]
    [InlineData(@"app.MapGet(""/"", () => new ValueTask<object>(new Todo() { Name = ""Test Item""}));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    [InlineData(@"app.MapGet(""/"", () => Task<object>.FromResult(new Todo() { Name = ""Test Item""}));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    [InlineData(@"app.MapGet(""/"", () => new ValueTask<object>(TypedResults.Ok(new Todo() { Name = ""Test Item""})));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    [InlineData(@"app.MapGet(""/"", () => Task<object>.FromResult(TypedResults.Ok(new Todo() { Name = ""Test Item""})));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    public async Task MapAction_NoParam_TaskLikeOfObjectReturn(string source, string expectedBody)
    {
        var (result, compilation) = await RunGeneratorAsync(source);

        var endpointModel = GetStaticEndpoint(result, GeneratorSteps.EndpointModelStep);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Equal("/", endpointModel.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);
        Assert.True(endpointModel.Response.IsAwaitable);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
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

        await VerifyAgainstBaselineUsingFile(compilation);

        var endpointModels = GetStaticEndpoints(results, GeneratorSteps.EndpointModelStep);

        Assert.Collection(endpointModels,
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
            });

        var endpoints = GetEndpointsFromCompilation(compilation);

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

    [Fact]
    public async Task MapAction_VariableRoutePattern_EmitsDiagnostic_NoSource()
    {
        var expectedBody = "Hello world!";
        var source = """
var route = "/en";
app.MapGet(route, () => "Hello world!");
""";
        var (result, compilation) = await RunGeneratorAsync(source);

        // Emits diagnostic but generates no source
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveRoutePattern.Id,diagnostic.Id);
        Assert.Empty(result.GeneratedSources);

        // Falls back to runtime-generated endpoint
        var endpoint = GetEndpointFromCompilation(compilation, expectSourceKey: false);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Fact]
    public async Task MapAction_UnknownParameter_EmitsDiagnostic_NoSource()
    {
        // This will eventually be handled by the EndpointParameterSource.JsonBodyOrService.
        // All parameters should theoretically be handleable with enough "Or"s in the future
        // we'll remove this test and diagnostic.
        var source = """
app.MapGet("/", (IServiceProvider provider) => "Hello world!");
""";
        var expectedBody = "Hello world!";
        var (result, compilation) = await RunGeneratorAsync(source);

        // Emits diagnostic but generates no source
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.GetUnableToResolveParameterDescriptor("provider").Id, diagnostic.Id);
        Assert.Empty(result.GeneratedSources);

        // Falls back to runtime-generated endpoint
        var endpoint = GetEndpointFromCompilation(compilation, expectSourceKey: false);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

    [Fact]
    public async Task MapAction_RequestDelegateHandler_DoesNotEmit()
    {
        var source = """
app.MapGet("/", (HttpContext context) => context.Response.WriteAsync("Hello world"));
""";
        var (result, _) = await RunGeneratorAsync(source);
        var endpointModels = GetStaticEndpoints(result, GeneratorSteps.EndpointModelStep);

        Assert.Empty(result.GeneratedSources);
        Assert.Empty(endpointModels);
    }
}
