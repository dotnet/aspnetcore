// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
{
    [Fact]
    public async Task RequestDelegatePopulatesHttpContextParameterWithoutAttribute()
    {
        var source = """
app.MapGet("/", (HttpContext httpContext) =>
{
    httpContext.Items["arg"] = httpContext;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);

        Assert.Same(httpContext, httpContext.Items["arg"]);
    }

    [Fact]
    public async Task RequestDelegatePassHttpContextRequestAbortedAsCancellationToken()
    {
        var source = """
app.MapGet("/", (HttpContext httpContext, System.Threading.CancellationToken token) =>
{
    httpContext.Items["arg"] = token;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        using var cts = new CancellationTokenSource();
        var httpContext = CreateHttpContext();
        // Reset back to default HttpRequestLifetimeFeature that implements a setter for RequestAborted.
        httpContext.Features.Set<IHttpRequestLifetimeFeature>(new HttpRequestLifetimeFeature());
        httpContext.RequestAborted = cts.Token;

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.RequestAborted, httpContext.Items["arg"]);
    }

    [Fact]
    public async Task RequestDelegatePassHttpContextUserAsClaimsPrincipal()
    {
        var source = """
app.MapGet("/", (HttpContext httpContext, System.Security.Claims.ClaimsPrincipal user) =>
{
    httpContext.Items["arg"] = user;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var user = new ClaimsPrincipal();
        var httpContext = CreateHttpContext();
        httpContext.User = user;

        await endpoint.RequestDelegate(httpContext);

        Assert.Same(user, httpContext.Items["arg"]);
    }

    [Fact]
    public async Task RequestDelegatePassHttpContextRequestAsHttpRequest()
    {
        var source = """
app.MapGet("/", (HttpContext httpContext, HttpRequest request) =>
{
    httpContext.Items["arg"] = request;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Request, httpContext.Items["arg"]);
    }

    [Fact]
    public async Task RequestDelegatePassesHttpContextResponseAsHttpResponse()
    {
        var source = """
app.MapGet("/", (HttpContext httpContext, HttpResponse response) =>
{
    httpContext.Items["arg"] = response;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(httpContext.Response, httpContext.Items["arg"]);
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

    public static object[][] DefaultValues
    {
        get
        {
            return
            [
                ["string?", "default", default(string), true],
                ["string", "\"test\"", "test", true],
                ["string", "\"a\" + \"b\"", "ab", true],
                ["DateOnly?", "default", default(DateOnly?), false],
                ["bool", "default", default(bool), true],
                ["bool", "false", false, true],
                ["bool", "true", true, true],
                ["System.Threading.CancellationToken", "default", default(CancellationToken), false],
                ["Todo?", "default", default(Todo), false],
                ["char", "\'a\'", 'a', true],
                ["int", "default", 0, true],
                ["int", "1234", 1234, true],
                ["int", "1234 * 4", 1234 * 4, true],
                ["double", "1.0", 1.0, true],
                ["double", "double.NaN", double.NaN, true],
                ["double", "double.PositiveInfinity", double.PositiveInfinity, true],
                ["double", "double.NegativeInfinity", double.NegativeInfinity, true],
                ["double", "double.E", double.E, true],
                ["double", "double.Epsilon", double.Epsilon, true],
                ["double", "double.NegativeZero", double.NegativeZero, true],
                ["double", "double.MaxValue", double.MaxValue, true],
                ["double", "double.MinValue", double.MinValue, true],
                ["double", "double.Pi", double.Pi, true],
                ["double", "double.Tau", double.Tau, true],
                ["float", "float.NaN", float.NaN, true],
                ["float", "float.PositiveInfinity", float.PositiveInfinity, true],
                ["float", "float.NegativeInfinity", float.NegativeInfinity, true],
                ["float", "float.E", float.E, true],
                ["float", "float.Epsilon", float.Epsilon, true],
                ["float", "float.NegativeZero", float.NegativeZero, true],
                ["float", "float.MaxValue", float.MaxValue, true],
                ["float", "float.MinValue", float.MinValue, true],
                ["float", "float.Pi", float.Pi, true],
                ["float", "float.Tau", float.Tau, true],
                ["decimal", "decimal.MaxValue", decimal.MaxValue, true],
                ["decimal", "decimal.MinusOne", decimal.MinusOne, true],
                ["decimal", "decimal.MinValue", decimal.MinValue, true],
                ["decimal", "decimal.One", decimal.One, true],
                ["decimal", "decimal.Zero", decimal.Zero, true],
                ["long", "long.MaxValue", long.MaxValue, true],
                ["long", "long.MinValue", long.MinValue, true],
                ["long", "(long)3.14", (long)3, true],
                ["short", "short.MaxValue", short.MaxValue, true],
                ["short", "short.MinValue", short.MinValue, true],
                ["ulong", "ulong.MaxValue", ulong.MaxValue, true],
                ["ulong", "ulong.MinValue", ulong.MinValue, true],
                ["ushort", "ushort.MaxValue", ushort.MaxValue, true],
                ["ushort", "ushort.MinValue", ushort.MinValue, true],
                ["TodoStatus", "TodoStatus.Done", TodoStatus.Done, true],
                ["TodoStatus", "TodoStatus.InProgress", TodoStatus.InProgress, true],
                ["TodoStatus", "TodoStatus.NotDone", TodoStatus.NotDone, true],
                ["TodoStatus", "(TodoStatus)1", TodoStatus.Done, true],
                ["MyEnum", "MyEnum.ValueA", MyEnum.ValueA, true],
                ["MyEnum", "MyEnum.ValueB", MyEnum.ValueB, true],
                // Test nullable enum values
                ["TodoStatus?", "TodoStatus.Done", (TodoStatus?)TodoStatus.Done, false],
                ["TodoStatus?", "default", default(TodoStatus?), false]
            ];
        }
    }

    [Theory]
    [MemberData(nameof(DefaultValues))]
    public async Task RequestDelegatePopulatesParametersWithDefaultValues(string type, string defaultValue, object expectedValue, bool declareConst)
    {
        var source = string.Empty;
        if (declareConst)
        {
            source = $$"""
const {{type}} defaultConst = {{defaultValue}};
static void TestAction(
    HttpContext context,
    {{type}} parameterWithDefault = {{defaultValue}},
    {{type}} parameterWithConst = defaultConst)
{
    context.Items.Add("parameterWithDefault", parameterWithDefault);
    context.Items.Add("parameterWithConst", parameterWithConst);
}
app.MapPost("/", TestAction);
""";
        }
        else
        {
            source = $$"""
static void TestAction(
HttpContext context,
{{type}} parameterWithDefault = {{defaultValue}})
{
context.Items.Add("parameterWithDefault", parameterWithDefault);
}
app.MapPost("/", TestAction);
""";
        }

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.User = new ClaimsPrincipal();

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(expectedValue, httpContext.Items["parameterWithDefault"]);
        if (declareConst)
        {
            Assert.Equal(expectedValue, httpContext.Items["parameterWithConst"]);
        }
    }

    [Fact]
    [UseCulture("fr-FR")]
    public async Task RequestDelegatePopulatesDecimalWithDefaultValuesAndCultureSet()
    {
        var source = $$"""
  const decimal defaultConst = 3.15m;
  static void TestAction(
      HttpContext context,
      decimal parameterWithDefault = 2.15m,
      decimal parameterWithConst = defaultConst)
  {
      context.Items.Add("parameterWithDefault", parameterWithDefault);
      context.Items.Add("parameterWithConst", parameterWithConst);
  }
  app.MapPost("/", TestAction);
  """;

        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.User = new ClaimsPrincipal();

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(2.15m, httpContext.Items["parameterWithDefault"]);
        Assert.Equal(3.15m, httpContext.Items["parameterWithConst"]);
    }
}
