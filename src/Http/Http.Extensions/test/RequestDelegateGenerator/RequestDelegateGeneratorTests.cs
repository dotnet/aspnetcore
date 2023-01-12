// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        var (results, compilation) = RunGenerator(source);

        var endpointModel = GetStaticEndpoint(results, GeneratorSteps.EndpointsStep);
        var endpoint = GetEndpointFromCompilation(compilation);
        var requestDelegate = endpoint.RequestDelegate;

        Assert.Equal("/hello", endpointModel.Route.RoutePattern);
        Assert.Equal(httpMethod, endpointModel.HttpMethod);

        var httpContext = new DefaultHttpContext();

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        await requestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
    }

    [Fact]
    public async Task MapGet_NoParam_StringReturn_WithFilter()
    {
        var source = """
app.MapGet("/hello", () => "Hello world!")
    .AddEndpointFilter(async (context, next) => {
        var result = await next(context);
        return $"Filtered: {result}";
    });
""";
        var expectedBody = "Filtered: Hello world!";
        var (results, compilation) = RunGenerator(source);

        await VerifyAgainstBaselineUsingFile(compilation);

        var endpointModel = GetStaticEndpoint(results, "EndpointModel");
        var endpoint = GetEndpointFromCompilation(compilation);
        var requestDelegate = endpoint.RequestDelegate;

        Assert.Equal("/hello", endpointModel.Route.RoutePattern);

        var httpContext = new DefaultHttpContext();

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        await requestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => 123456);", "123456")]
    [InlineData(@"app.MapGet(""/"", () => true);", "true")]
    [InlineData(@"app.MapGet(""/"", () => new DateTime(2023, 1, 1));", @"""2023-01-01T00:00:00""")]
    public async Task MapAction_NoParam_AnyReturn(string source, string expectedBody)
    {
        var (results, compilation) = RunGenerator(source);

        var endpointModel = GetStaticEndpoint(results, GeneratorSteps.EndpointsStep);
        var endpoint = GetEndpointFromCompilation(compilation);
        var requestDelegate = endpoint.RequestDelegate;

        Assert.Equal("/", endpointModel.Route.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);

        var httpContext = new DefaultHttpContext();

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        await requestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
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
        var (results, compilation) = RunGenerator(source);

        var endpointModel = GetStaticEndpoint(results, GeneratorSteps.EndpointsStep);
        var endpoint = GetEndpointFromCompilation(compilation);
        var requestDelegate = endpoint.RequestDelegate;

        Assert.Equal("/", endpointModel.Route.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);

        var httpContext = CreateHttpContext();

        await requestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => Console.WriteLine(""Returns void""));", null)]
    [InlineData(@"app.MapGet(""/"", () => TypedResults.Ok(""Alright!""));", null)]
    [InlineData(@"app.MapGet(""/"", () => Results.NotFound(""Oops!""));", null)]
    [InlineData(@"app.MapGet(""/"", () => Task.FromResult(new Todo() { Name = ""Test Item""}));", "application/json")]
    [InlineData(@"app.MapGet(""/"", () => ""Hello world!"");", "text/plain")]
    public void MapAction_ProducesCorrectContentType(string source, string expectedContentType)
    {
        var (results, compilation) = RunGenerator(source);

        var endpointModel = GetStaticEndpoint(results, GeneratorSteps.EndpointsStep);

        Assert.Equal("/", endpointModel.Route.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);
        Assert.Equal(expectedContentType, endpointModel.Response.ContentType);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => Task.FromResult(""Hello world!""));", "Hello world!")]
    [InlineData(@"app.MapGet(""/"", () => Task.FromResult(new Todo() { Name = ""Test Item""}));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    [InlineData(@"app.MapGet(""/"", () => Task.FromResult(TypedResults.Ok(new Todo() { Name = ""Test Item""})));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    public async Task MapAction_NoParam_TaskOfTReturn(string source, string expectedBody)
    {
        var (results, compilation) = RunGenerator(source);

        var endpointModel = GetStaticEndpoint(results, GeneratorSteps.EndpointsStep);
        var endpoint = GetEndpointFromCompilation(compilation);
        var requestDelegate = endpoint.RequestDelegate;

        Assert.Equal("/", endpointModel.Route.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);
        Assert.True(endpointModel.Response.IsAwaitable);

        var httpContext = CreateHttpContext();

        await requestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
    }

    [Theory]
    [InlineData(@"app.MapGet(""/"", () => ValueTask.FromResult(""Hello world!""));", "Hello world!")]
    [InlineData(@"app.MapGet(""/"", () => ValueTask.FromResult(new Todo() { Name = ""Test Item""}));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    [InlineData(@"app.MapGet(""/"", () => ValueTask.FromResult(TypedResults.Ok(new Todo() { Name = ""Test Item""})));", """{"id":0,"name":"Test Item","isComplete":false}""")]
    public async Task MapAction_NoParam_ValueTaskOfTReturn(string source, string expectedBody)
    {
        var (results, compilation) = RunGenerator(source);

        var endpointModel = GetStaticEndpoint(results, GeneratorSteps.EndpointsStep);
        var endpoint = GetEndpointFromCompilation(compilation);
        var requestDelegate = endpoint.RequestDelegate;

        Assert.Equal("/", endpointModel.Route.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);
        Assert.True(endpointModel.Response.IsAwaitable);

        var httpContext = CreateHttpContext();

        await requestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
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
        var (results, compilation) = RunGenerator(source);

        var endpointModel = GetStaticEndpoint(results, GeneratorSteps.EndpointsStep);
        var endpoint = GetEndpointFromCompilation(compilation);
        var requestDelegate = endpoint.RequestDelegate;

        Assert.Equal("/", endpointModel.Route.RoutePattern);
        Assert.Equal("MapGet", endpointModel.HttpMethod);
        Assert.True(endpointModel.Response.IsAwaitable);

        var httpContext = CreateHttpContext();

        await requestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
    }

    [Fact]
    public async Task Multiple_MapAction_NoParam_StringReturn()
    {
        var source = """
app.MapGet("/en", () => "Hello world!");
app.MapGet("/es", () => "Hola mundo!");
""";
        var (_, compilation) = RunGenerator(source);

        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_VariableRoutePattern_EmitsDiagnostic_NoSource()
    {
        var expectedBody = "Hello world!";
        var source = """
var route = "/en";
app.MapGet(route, () => "Hello world!");
""";
        var (results, compilation) = RunGenerator(source);

        // Emits diagnostic but generates no source
        var result = Assert.Single(results);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.UnableToResolveRoutePattern.Id,diagnostic.Id);
        Assert.Empty(result.GeneratedSources);

        // Falls back to runtime-generated endpoint
        var endpoint = GetEndpointFromCompilation(compilation, checkSourceKey: false);
        var requestDelegate = endpoint.RequestDelegate;

        var httpContext = CreateHttpContext();

        await requestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedBody, body);
    }
}
