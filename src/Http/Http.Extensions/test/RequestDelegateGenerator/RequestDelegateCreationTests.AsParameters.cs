// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class RequestDelegateCreationTests
{
    [Fact]
    public async Task RequestDelegatePopulatesFromRouteParameterBased_FromParameterList()
    {
        const int originalRouteParam = 42;

        var source = """
static void TestAction([AsParameters] ParameterListFromRoute args)
{
    args.HttpContext.Items["input"] = args.Value;
}
app.MapGet("/{Value}", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["value"] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

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

    [Fact]
    public async Task RequestDelegatePopulatesHttpContextParametersWithoutAttribute_FromParameterList()
    {
        var source = """
static void TestAction([AsParameters] ParametersListWithHttpContext args)
{
    args.HttpContext.Items.Add("input", args.HttpContext);
    args.HttpContext.Items.Add("user", args.User);
    args.HttpContext.Items.Add("request", args.Request);
    args.HttpContext.Items.Add("response", args.Response);
}
app.MapGet("/", TestAction);
""";

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.User = new ClaimsPrincipal();

        await endpoint.RequestDelegate(httpContext);

        Assert.Same(httpContext, httpContext.Items["input"]);
        Assert.Same(httpContext.User, httpContext.Items["user"]);
        Assert.Same(httpContext.Request, httpContext.Items["request"]);
        Assert.Same(httpContext.Response, httpContext.Items["response"]);
    }

    public static object[][] FromParameterListActions
    {
        get
        {
            var TestParameterListRecordStruct = """
void TestAction([AsParameters] ParameterListRecordStruct args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
""";

            var TestParameterListRecordClass = """
void TestAction([AsParameters] ParameterListRecordClass args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
""";

            var TestParameterListRecordWithoutPositionalParameters = """
void TestAction([AsParameters] ParameterListRecordWithoutPositionalParameters args)
{
    args.HttpContext!.Items.Add("input", args.Value);
}
""";

            var TestParameterListStruct = """
void TestAction([AsParameters] ParameterListStruct args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
""";

            var TestParameterListMutableStruct = """
void TestAction([AsParameters] ParameterListMutableStruct args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
""";

            var TestParameterListStructWithParameterizedContructor = """
void TestAction([AsParameters] ParameterListStructWithParameterizedContructor args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
""";

            var TestParameterListStructWithMultipleParameterizedContructor = """
void TestAction([AsParameters] ParameterListStructWithMultipleParameterizedContructor args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
""";

            var TestParameterListClass = """
void TestAction([AsParameters] ParameterListClass args)
{
    args.HttpContext!.Items.Add("input", args.Value);
}
""";

            var TestParameterListClassWithParameterizedContructor = """
void TestAction([AsParameters] ParameterListClassWithParameterizedContructor args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
""";

            return new[]
            {
                new object[] { TestParameterListRecordStruct },
                new object[] { TestParameterListRecordClass },
                new object[] { TestParameterListRecordWithoutPositionalParameters },
                new object[] { TestParameterListStruct },
                new object[] { TestParameterListMutableStruct },
                new object[] { TestParameterListStructWithParameterizedContructor },
                new object[] { TestParameterListStructWithMultipleParameterizedContructor },
                new object[] { TestParameterListClass },
                new object[] { TestParameterListClassWithParameterizedContructor },
            };
        }
    }

    [Theory]
    [MemberData(nameof(FromParameterListActions))]
    public async Task RequestDelegatePopulatesFromParameterList(string innerSource)
    {
        var source = $$"""
{{innerSource}}
app.MapGet("/{value}", TestAction);
""";
        const string paramName = "value";
        const int originalRouteParam = 42;

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(originalRouteParam, httpContext.Items["input"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromParameterListUsesDefaultValue()
    {
        const int expectedValue = 42;
        var source = """
void TestAction([AsParameters] ParameterListWitDefaultValue args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
app.MapGet("/{value}", TestAction);
""";
        var httpContext = CreateHttpContext();

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(expectedValue, httpContext.Items["input"]);
    }

    [Fact]
    public async Task VerifyAsParametersBaseline()
    {
        var (_, compilation) = await RunGeneratorAsync("""
void parameterListWithDefaultValue([AsParameters] ParameterListWitDefaultValue args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
void parameterListRecordStruct([AsParameters] ParameterListRecordStruct args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
static void parametersListWithHttpContext([AsParameters] ParametersListWithHttpContext args)
{
    args.HttpContext.Items.Add("input", args.HttpContext);
    args.HttpContext.Items.Add("user", args.User);
    args.HttpContext.Items.Add("request", args.Request);
    args.HttpContext.Items.Add("response", args.Response);
}
app.MapGet("/parameterListWithDefaultValue/{value}", parameterListWithDefaultValue);
app.MapGet("/parameterListRecordStruct/{value}", parameterListRecordStruct);
app.MapGet("/parametersListWithHttpContext", parametersListWithHttpContext);
app.MapPost("/parametersListWithImplicitFromBody", ([AsParameters] ParametersListWithImplicitFromBody args) => args.Todo.Name ?? string.Empty);
""");

        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task RequestDelegateHandlesReadOnlyProperties()
    {
        const int expectedValue = 32;
        var source = """
void TestAction([AsParameters] ParameterListWithReadOnlyProperty args)
{
    args.HttpContext.Items.Add("input", args.Value);
}
app.MapGet("/{value}", TestAction);
""";
        const string paramName = "value";
        const int originalRouteParam = 42;

        var httpContext = CreateHttpContext();

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(expectedValue, httpContext.Items["input"]);
    }
}
