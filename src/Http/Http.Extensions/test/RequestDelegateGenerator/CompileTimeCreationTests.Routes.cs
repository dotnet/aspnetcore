// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class CompileTimeCreationTests : RequestDelegateCreationTests
{
    [Fact]
    public async Task SupportsRoutePatternInVariable()
    {
        var source = """
var route = "/hello";
app.MapGet(route, () =>
{
    return "Hello world!";
});
""";
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
    }

    [Fact]
    public async Task SupportsRoutePatternInConst()
    {
        var source = """
const string route = "/hello";
app.MapGet(route, () =>
{
    return "Hello world!";
});
""";
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "Hello world!");
    }

    [Fact]
    public async Task SupportsComputedRoutePattern()
    {
        var source = """
for (int i = 0; i < 5; i++)
{
    var route = $"/hello/{i}";
    app.MapGet(route, () =>
    {
        return $"Hello world!";
    });
}
""";
        var (result, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        VerifyStaticEndpointModel(result, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
        });

        for (int i = 0; i < 5; i++)
        {
            var endpoint = endpoints[i];
            var httpContext = CreateHttpContext();
            await endpoint.RequestDelegate(httpContext);
            await VerifyResponseBodyAsync(httpContext, $"Hello world!");
        }

    }
}
