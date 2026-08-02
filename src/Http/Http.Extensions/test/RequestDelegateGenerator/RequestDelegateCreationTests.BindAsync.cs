// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
{
    public static object[][] BindAsyncUriTypesAndOptionalitySupport = new object[][]
    {
        new object[] { "MyBindAsyncRecord", false },
        new object[] { "MyBindAsyncStruct", true },
        new object[] { "MyNullableBindAsyncStruct", false },
        new object[] { "MyBothBindAsyncStruct", true },
        new object[] { "MySimpleBindAsyncRecord", false, },
        new object[] { "MySimpleBindAsyncStruct", true },
        new object[] { "BindAsyncFromImplicitStaticAbstractInterface", false },
        new object[] { "InheritBindAsync", false },
        new object[] { "BindAsyncFromExplicitStaticAbstractInterface", false },
        new object[] { "MyBindAsyncFromInterfaceRecord", false },
    };

    public static IEnumerable<object[]> BindAsyncUriTypes =>
        BindAsyncUriTypesAndOptionalitySupport.Select(x => new[] { x[0] });

    [Theory]
    [MemberData(nameof(BindAsyncUriTypes))]
    public async Task MapAction_BindAsync_Optional_Provided(string bindAsyncType)
    {
        var source = $$"""
app.MapGet("/", (HttpContext httpContext, {{bindAsyncType}}? myBindAsyncParam) =>
{
    httpContext.Items["uri"] = myBindAsyncParam?.Uri;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Referer = "https://example.org";
        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(new Uri("https://example.org"), httpContext.Items["uri"]);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(BindAsyncUriTypes))]
    public async Task MapAction_BindAsync_NonOptional_Provided(string bindAsyncType)
    {
        var source = $$"""
app.MapGet("/", (HttpContext httpContext, {{bindAsyncType}} myBindAsyncParam) =>
{
    httpContext.Items["uri"] = myBindAsyncParam.Uri;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Referer = "https://example.org";
        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(new Uri("https://example.org"), httpContext.Items["uri"]);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(BindAsyncUriTypesAndOptionalitySupport))]
    public async Task MapAction_BindAsync_Optional_NotProvided(string bindAsyncType, bool expectException)
    {
        var source = $$"""
app.MapGet("/", (HttpContext httpContext, {{bindAsyncType}}? myBindAsyncParam) =>
{
    httpContext.Items["uri"] = myBindAsyncParam?.Uri;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        if (expectException)
        {
            // These types simply don't support optional parameters since they cannot return null.
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));
            Assert.Equal("The request is missing the required Referer header.", ex.Message);
        }
        else
        {
            await endpoint.RequestDelegate(httpContext);

            Assert.Null(httpContext.Items["uri"]);
            Assert.Equal(200, httpContext.Response.StatusCode);
        }
    }

    [Theory]
    [MemberData(nameof(BindAsyncUriTypesAndOptionalitySupport))]
    public async Task MapAction_BindAsync_NonOptional_NotProvided(string bindAsyncType, bool expectException)
    {
        var source = $$"""
app.MapGet("/", (HttpContext httpContext, {{bindAsyncType}} myBindAsyncParam) =>
{
    httpContext.Items["uri"] = myBindAsyncParam.Uri;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        if (expectException)
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));
            Assert.Equal("The request is missing the required Referer header.", ex.Message);
        }
        else
        {
            await endpoint.RequestDelegate(httpContext);

            Assert.Null(httpContext.Items["uri"]);
            Assert.Equal(400, httpContext.Response.StatusCode);
            Assert.Equal(400, httpContext.Response.StatusCode);
            var log = Assert.Single(TestSink.Writes);
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
            var parameters = bindAsyncType is "MySimpleBindAsyncRecord" || bindAsyncType is "InheritBindAsync" || bindAsyncType is "MyBindAsyncFromInterfaceRecord"
                ? "(HttpContext)"
                : "(HttpContext, ParameterInfo)";
            Assert.Equal($@"Required parameter ""{bindAsyncType} myBindAsyncParam"" was not provided from {bindAsyncType}.BindAsync{parameters}.", log.Message);
        }
    }

    [Fact]
    public async Task MapAction_BindAsync_Snapshot()
    {
        var source = new StringBuilder();

        var i = 0;
        while (i < BindAsyncUriTypesAndOptionalitySupport.Length * 2)
        {
            var bindAsyncType = BindAsyncUriTypesAndOptionalitySupport[i / 2][0];
            source.AppendLine(CultureInfo.InvariantCulture, $$"""app.MapGet("/{{i}}", (HttpContext httpContext, {{bindAsyncType}} myBindAsyncParam) => "Hello world! {{i}}");""");
            i++;
            source.AppendLine(CultureInfo.InvariantCulture, $$"""app.MapGet("/{{i}}", ({{bindAsyncType}}? myBindAsyncParam) => "Hello world! {{i}}");""");
            i++;
        }

        var (_, compilation) = await RunGeneratorAsync(source.ToString());

        await VerifyAgainstBaselineUsingFile(compilation);

        var endpoints = GetEndpointsFromCompilation(compilation);
        Assert.Equal(BindAsyncUriTypesAndOptionalitySupport.Length * 2, endpoints.Length);

        for (i = 0; i < BindAsyncUriTypesAndOptionalitySupport.Length * 2; i++)
        {
            var httpContext = CreateHttpContext();
            // Set a referrer header so BindAsync always succeeds and the route handler is always called optional or not.
            httpContext.Request.Headers.Referer = "https://example.org";

            await endpoints[i].RequestDelegate(httpContext);
            await VerifyResponseBodyAsync(httpContext, $"Hello world! {i}");
        }
    }

    [Fact]
    public async Task MapAction_BindAsync_ExceptionsAreUncaught()
    {
        var source = """
app.MapGet("/", (HttpContext httpContext, MyBindAsyncTypeThatThrows myBindAsyncParam) => { });
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Referer = "https://example.org";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => endpoint.RequestDelegate(httpContext));
        Assert.Equal("BindAsync failed", ex.Message);
    }

    [Fact]
    public async Task BindAsyncWithBodyArgument()
    {
        Todo originalTodo = new()
        {
            Name = "Write more tests!"
        };

        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var jsonOptions = new JsonOptions();
        jsonOptions.SerializerOptions.Converters.Add(new TodoJsonConverter());

        var mock = new Mock<IServiceProvider>();
        mock.Setup(m => m.GetService(It.IsAny<Type>())).Returns<Type>(t =>
        {
            if (t == typeof(IOptions<JsonOptions>))
            {
                return Options.Create(jsonOptions);
            }
            return null;
        });

        httpContext.RequestServices = mock.Object;
        httpContext.Request.Headers.Referer = "https://example.org";

        var source = """
app.MapPost("/", (HttpContext context, MyBindAsyncRecord myBindAsyncParam, Todo todo) =>
{
    context.Items["invoked"] = true;
    context.Items[nameof(myBindAsyncParam)] = myBindAsyncParam;
    context.Items[nameof(todo)] = todo;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        await endpoint.RequestDelegate(httpContext);

        Assert.True(httpContext.Items["invoked"] as bool?);
        var arg = httpContext.Items["myBindAsyncParam"] as MyBindAsyncRecord;
        Assert.NotNull(arg);
        Assert.Equal("https://example.org/", arg!.Uri.ToString());
        var todo = httpContext.Items["todo"] as Todo;
        Assert.NotNull(todo);
        Assert.Equal("Write more tests!", todo!.Name);
    }

    [Fact]
    public async Task BindAsyncRunsBeforeBodyBinding()
    {
        Todo originalTodo = new()
        {
            Name = "Write more tests!"
        };

        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var jsonOptions = new JsonOptions();
        jsonOptions.SerializerOptions.Converters.Add(new TodoJsonConverter());

        var mock = new Mock<IServiceProvider>();
        mock.Setup(m => m.GetService(It.IsAny<Type>())).Returns<Type>(t =>
        {
            if (t == typeof(IOptions<JsonOptions>))
            {
                return Options.Create(jsonOptions);
            }
            return null;
        });

        httpContext.RequestServices = mock.Object;
        httpContext.Request.Headers.Referer = "https://example.org";

        var source = """
app.MapPost("/", (HttpContext context, CustomTodo customTodo, Todo todo) =>
{
    context.Items["invoked"] = true;
    context.Items[nameof(customTodo)] = customTodo;
    context.Items[nameof(todo)] = todo;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        await endpoint.RequestDelegate(httpContext);

        Assert.True(httpContext.Items["invoked"] as bool?);
        var todo0 = httpContext.Items["customTodo"] as Todo;
        Assert.NotNull(todo0);
        Assert.Equal("Write more tests!", todo0!.Name);
        var todo1 = httpContext.Items["todo"] as Todo;
        Assert.NotNull(todo1);
        Assert.Equal("Write more tests!", todo1!.Name);
    }

    [Fact]
    public async Task MapAction_BindAsync_MismatchedNullability()
    {
        var source = """
app.MapGet("/1", (BindableWithMismatchedNullability<Todo> param) => "Hello /1!");
app.MapGet("/2", (BindableStructWithMismatchedNullability<Todo> param) => "Hello /2!");
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoints = GetEndpointsFromCompilation(compilation);

        var index = 1;
        foreach (var endpoint in endpoints)
        {
            var httpContext = CreateHttpContext();
            await endpoint.RequestDelegate(httpContext);
            await VerifyResponseBodyAsync(httpContext, $"Hello /{index}!");
            index++;
        }
    }
}
