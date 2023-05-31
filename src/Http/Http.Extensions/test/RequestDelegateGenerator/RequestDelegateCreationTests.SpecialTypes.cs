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
}
