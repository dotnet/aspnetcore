// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class RequestDelegateCreationTests
{
    [Fact]
    public async Task RequestDelegatePopulatesFromRouteParameterBased_FromParameterList()
    {
        const string paramName = "value";
        const int originalRouteParam = 42;

        var source = """
static void TestAction([AsParameters] ParameterListFromRoute args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
app.MapGet("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(originalRouteParam, httpContext.Items["input"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromQueryParameter_FromParameterList()
    {
        // QueryCollection is case sensitve, since we now getting
        // the parameter name from the Property/Record constructor
        // we should match the case here
        const string paramName = "Value";
        const int originalQueryParam = 42;

        var source = """
static void TestAction([AsParameters] ParameterListFromQuery args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
app.MapGet("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var query = new QueryCollection(new Dictionary<string, StringValues>()
        {
            [paramName] = originalQueryParam.ToString(NumberFormatInfo.InvariantInfo)
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = query;

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(originalQueryParam, httpContext.Items["input"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromHeaderParameter_FromParameterList()
    {
        const string customHeaderName = "X-Custom-Header";
        const int originalHeaderParam = 42;

        var source = """
static void TestAction([AsParameters] ParameterListFromHeader args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
app.MapGet("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[customHeaderName] = originalHeaderParam.ToString(NumberFormatInfo.InvariantInfo);

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(originalHeaderParam, httpContext.Items["input"]);
    }
}
