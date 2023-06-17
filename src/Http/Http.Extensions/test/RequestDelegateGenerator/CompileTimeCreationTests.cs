// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class CompileTimeCreationTests : RequestDelegateCreationTests
{
    protected override bool IsGeneratorEnabled { get; } = true;

    [Fact]
    public async Task MapGet_WithRequestDelegate_DoesNotGenerateSources()
    {
        var (generatorRunResult, compilation) = await RunGeneratorAsync("""
app.MapGet("/hello", (HttpContext context) => Task.CompletedTask);
""");
        var results = Assert.IsType<GeneratorRunResult>(generatorRunResult);
        Assert.Empty(GetStaticEndpoints(results, GeneratorSteps.EndpointModelStep));

        var endpoint = GetEndpointFromCompilation(compilation, false);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "");
    }

    [Fact]
    public async Task MapAction_ExplicitRouteParamWithInvalidName_SimpleReturn()
    {
        var source = $$"""app.MapGet("/{routeValue}", ([FromRoute(Name = "invalidName" )] string parameterName) => parameterName);""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => endpoint.RequestDelegate(httpContext));
        Assert.Equal("'invalidName' is not a route parameter.", exception.Message);
    }

    [Theory]
    [InlineData("""app.MapGet("/", () => Microsoft.FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return("Hello"));""")]
    [InlineData("""app.MapGet("/", () => Microsoft.FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new Todo { Name = "Hello" }));""")]
    [InlineData("""app.MapGet("/", () => Microsoft.FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(TypedResults.Ok(new Todo { Name = "Hello" })));""")]
    [InlineData("""app.MapGet("/", () => Microsoft.FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(default(Microsoft.FSharp.Core.Unit)!));""")]
    public async Task MapAction_NoParam_FSharpAsyncReturn_NotCoercedToTaskAtCompileTime(string source)
    {
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            Assert.False(endpointModel.Response.IsAwaitable);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody: "{}");
    }

    [Theory]
    [InlineData("""app.MapGet("/", () => Task.FromResult(default(Microsoft.FSharp.Core.Unit)!));""")]
    [InlineData("""app.MapGet("/", () => ValueTask.FromResult(default(Microsoft.FSharp.Core.Unit)!));""")]
    public async Task MapAction_NoParam_TaskLikeOfUnitReturn_NotConvertedToVoidReturningAtCompileTime(string source)
    {
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            Assert.True(endpointModel.Response.IsAwaitable);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody: "null");
    }
}
