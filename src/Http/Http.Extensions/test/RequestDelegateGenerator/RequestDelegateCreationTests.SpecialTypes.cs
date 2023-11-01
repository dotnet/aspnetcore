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
            return new[]
            {
                new object[] { "string?", "default", default(string), true },
                new object[] { "string", "\"test\"", "test", true },
                new object[] { "string", "\"a\" + \"b\"", "ab", true },
                new object[] { "DateOnly?", "default", default(DateOnly?), false },
                new object[] { "bool", "default", default(bool), true },
                new object[] { "bool", "false", false, true },
                new object[] { "bool", "true", true, true},
                new object[] { "System.Threading.CancellationToken", "default", default(CancellationToken), false },
                new object[] { "Todo?", "default", default(Todo), false },
                new object[] { "char", "\'a\'", 'a', true },
                new object[] { "int", "default", 0, true },
                new object[] { "int", "1234", 1234, true },
                new object[] { "int", "1234 * 4", 1234 * 4, true },
                new object[] { "double", "1.0", 1.0, true },
                new object[] { "double", "double.NaN", double.NaN, true },
                new object[] { "double", "double.PositiveInfinity", double.PositiveInfinity, true },
                new object[] { "double", "double.NegativeInfinity", double.NegativeInfinity, true },
                new object[] { "double", "double.E", double.E, true },
                new object[] { "double", "double.Epsilon", double.Epsilon, true },
                new object[] { "double", "double.NegativeZero", double.NegativeZero, true },
                new object[] { "double", "double.MaxValue", double.MaxValue, true },
                new object[] { "double", "double.MinValue", double.MinValue, true },
                new object[] { "double", "double.Pi", double.Pi, true },
                new object[] { "double", "double.Tau", double.Tau, true },
                new object[] { "float", "float.NaN", float.NaN, true },
                new object[] { "float", "float.PositiveInfinity", float.PositiveInfinity, true },
                new object[] { "float", "float.NegativeInfinity", float.NegativeInfinity, true },
                new object[] { "float", "float.E", float.E, true },
                new object[] { "float", "float.Epsilon", float.Epsilon, true },
                new object[] { "float", "float.NegativeZero", float.NegativeZero, true },
                new object[] { "float", "float.MaxValue", float.MaxValue, true },
                new object[] { "float", "float.MinValue", float.MinValue, true },
                new object[] { "float", "float.Pi", float.Pi, true },
                new object[] { "float", "float.Tau", float.Tau, true },
                new object[] {"decimal", "decimal.MaxValue", decimal.MaxValue, true },
                new object[] {"decimal", "decimal.MinusOne", decimal.MinusOne, true },
                new object[] {"decimal", "decimal.MinValue", decimal.MinValue, true },
                new object[] {"decimal", "decimal.One", decimal.One, true },
                new object[] {"decimal", "decimal.Zero", decimal.Zero, true },
                new object[] {"long", "long.MaxValue", long.MaxValue, true },
                new object[] {"long", "long.MinValue", long.MinValue, true },
                new object[] {"short", "short.MaxValue", short.MaxValue, true },
                new object[] {"short", "short.MinValue", short.MinValue, true },
                new object[] {"ulong", "ulong.MaxValue", ulong.MaxValue, true },
                new object[] {"ulong", "ulong.MinValue", ulong.MinValue, true },
                new object[] {"ushort", "ushort.MaxValue", ushort.MaxValue, true },
                new object[] {"ushort", "ushort.MinValue", ushort.MinValue, true },
            };
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
