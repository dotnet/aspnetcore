// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.Extensions.Primitives;

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

    [Fact]
    public async Task MapAction_SupportsParametersWithDifferingNullability()
    {
        var source = """
app.MapGet("/hello", (string name) => $"Hello {name}!");
app.MapGet("/hello2", (string? name) => $"Hello {name ?? string.Empty}!");
""";
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        foreach (var endpoint in endpoints)
        {
            var httpContext = CreateHttpContext();
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>()
            {
                {
                    "name", "world"
                }
            });
            await endpoint.RequestDelegate(httpContext);
            await VerifyResponseBodyAsync(httpContext, "Hello world!");
        }
    }
}
