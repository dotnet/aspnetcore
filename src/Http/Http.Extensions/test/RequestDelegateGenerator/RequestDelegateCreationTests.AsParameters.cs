// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
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
        const string customParamName = "customQuery";
        const int originalCustomQueryParam = 43;
        const string anotherCustomParamName = "anotherCustomQuery";
        const int originalAnotherCustomQueryParam = 44;

        var source = """
static void TestAction([AsParameters] ParameterListFromQuery args)
{
    args.HttpContext.Items.Add("input", args.Value);
    args.HttpContext.Items.Add("customInput", args.CustomValue);
    args.HttpContext.Items.Add("anotherCustomInput", args.AnotherCustomValue);
}
app.MapGet("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var query = new QueryCollection(new Dictionary<string, StringValues>()
        {
            [paramName] = originalQueryParam.ToString(NumberFormatInfo.InvariantInfo),
            [customParamName] = originalCustomQueryParam.ToString(NumberFormatInfo.InvariantInfo),
            [anotherCustomParamName] = originalAnotherCustomQueryParam.ToString(NumberFormatInfo.InvariantInfo)
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = query;

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(originalQueryParam, httpContext.Items["input"]);
        Assert.Equal(originalCustomQueryParam, httpContext.Items["customInput"]);
        Assert.Equal(originalAnotherCustomQueryParam, httpContext.Items["anotherCustomInput"]);
    }

    [Theory]
    [InlineData("ParameterListFromHeader")]
    [InlineData("ParameterListFromHeaderWithProperties")]
    public async Task RequestDelegatePopulatesFromHeaderParameter_FromParameterList(string type)
    {
        const string customHeaderName = "X-Custom-Header";
        const int originalHeaderParam = 42;

        var source = $$"""
static void TestAction([AsParameters] {{type}} args)
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
static void parametersListWithMetadataType([AsParameters] ParametersListWithMetadataType args)
{
    args.HttpContext.Items.Add("value", args.Value);
}
app.MapGet("/parameterListWithDefaultValue/{value}", parameterListWithDefaultValue);
app.MapPost("/parameterListRecordStruct/{value}", parameterListRecordStruct);
app.MapPut("/parametersListWithHttpContext", parametersListWithHttpContext);
app.MapPatch("/parametersListWithImplicitFromBody", ([AsParameters] ParametersListWithImplicitFromBody args) => args.Todo.Name ?? string.Empty);
app.MapGet("/parametersListWithMetadataType", parametersListWithMetadataType);
app.MapPost("/parameterRecordStructWithJsonBodyOrService", ([AsParameters] ParameterRecordStructWithJsonBodyOrService args) => args.Todo.Name ?? string.Empty);
""");

        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromParameterListAndSkipReadOnlyProperties()
    {
        const int routeParamValue = 42;
        var expectedInput = new ParameterListWithReadOnlyProperties() { Value = routeParamValue };

        var source = """
void TestAction(HttpContext context, [AsParameters] ParameterListWithReadOnlyProperties args)
{
    context.Items.Add("input", args);
}
app.MapGet("/{value}", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        httpContext.Request.RouteValues[nameof(ParameterListWithReadOnlyProperties.Value)] = routeParamValue.ToString(NumberFormatInfo.InvariantInfo);
        httpContext.Request.RouteValues[nameof(ParameterListWithReadOnlyProperties.ConstantValue)] = routeParamValue.ToString(NumberFormatInfo.InvariantInfo);
        httpContext.Request.RouteValues[nameof(ParameterListWithReadOnlyProperties.ReadOnlyValue)] = routeParamValue.ToString(NumberFormatInfo.InvariantInfo);
        httpContext.Request.RouteValues[nameof(ParameterListWithReadOnlyProperties.PrivateSetValue)] = routeParamValue.ToString(NumberFormatInfo.InvariantInfo);

        await endpoint.RequestDelegate(httpContext);

        var input = Assert.IsType<ParameterListWithReadOnlyProperties>(httpContext.Items["input"]);
        Assert.Equal(expectedInput.Value, input.Value);
        Assert.Equal(expectedInput.ConstantValue, input.ConstantValue);
        Assert.Equal(expectedInput.ReadOnlyValue, input.ReadOnlyValue);
        Assert.Equal(expectedInput.PrivateSetValue, input.PrivateSetValue);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromMultipleParameterLists()
    {
        const int foo = 1;
        const int bar = 2;

        var source = """
void TestAction(HttpContext context, [AsParameters] SampleParameterList args, [AsParameters] AdditionalSampleParameterList args2)
{
    context.Items.Add("foo", args.Foo);
    context.Items.Add("bar", args2.Bar);
}
app.MapGet("/{foo}/{bar}", TestAction);
""";

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues[nameof(SampleParameterList.Foo)] = foo.ToString(NumberFormatInfo.InvariantInfo);
        httpContext.Request.RouteValues[nameof(AdditionalSampleParameterList.Bar)] = bar.ToString(NumberFormatInfo.InvariantInfo);

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(foo, httpContext.Items["foo"]);
        Assert.Equal(bar, httpContext.Items["bar"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromBindAsyncParameterList()
    {
        const string uriValue = "https://example.org/";

        var source = """
void TestAction([AsParameters] ParametersListWithBindAsyncType args)
{
    args.HttpContext.Items.Add("value", args.Value);
    args.HttpContext.Items.Add("anotherValue", args.MyBindAsyncParam);
}
app.MapGet("/", TestAction);
""";

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Referer = uriValue;

        await endpoint.RequestDelegate(httpContext);

        var value = Assert.IsType<InheritBindAsync>(httpContext.Items["value"]);
        var anotherValue = Assert.IsType<MyBindAsyncRecord>(httpContext.Items["anotherValue"]);

        Assert.Equal(uriValue, value.Uri?.ToString());
        Assert.Equal(uriValue, anotherValue.Uri?.ToString());
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromMetadataProviderParameterList()
    {
        var source = """
void TestAction([AsParameters] ParametersListWithMetadataType args)
{
    args.HttpContext.Items.Add("value", args.Value);
}
app.MapPost("/", TestAction);
""";

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        Assert.Contains(endpoint.Metadata, m => m is ParameterNameMetadata { Name: "Value" });
    }

    [Fact]
    public async Task RequestDelegateFactory_AsParameters_SupportsRequiredMember()
    {
        // Arrange
        var source = """
app.MapGet("/{requiredRouteParam}", TestAction);
static void TestAction([AsParameters] ParameterListRequiredStringFromDifferentSources args) { }
""";

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        // Act
        await endpoint.RequestDelegate(httpContext);

        // Assert that the required modifier on members that
        // are not nullable treats them as required.
        Assert.Equal(400, httpContext.Response.StatusCode);

        var logs = TestSink.Writes.ToArray();

        Assert.Equal(3, logs.Length);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[0].EventId);
        Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
        Assert.Equal(@"Required parameter ""string RequiredRouteParam"" was not provided from route.", logs[0].Message);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[1].EventId);
        Assert.Equal(LogLevel.Debug, logs[1].LogLevel);
        Assert.Equal(@"Required parameter ""string RequiredQueryParam"" was not provided from query string.", logs[1].Message);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[2].EventId);
        Assert.Equal(LogLevel.Debug, logs[2].LogLevel);
        Assert.Equal(@"Required parameter ""string RequiredHeaderParam"" was not provided from header.", logs[2].Message);
    }
}
