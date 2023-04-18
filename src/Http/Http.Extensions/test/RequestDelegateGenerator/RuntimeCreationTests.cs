// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public class RuntimeCreationTests : RequestDelegateCreationTests
{
    protected override bool IsGeneratorEnabled { get; } = false;

    [Theory]
    [InlineData("BindAsyncWrongType")]
    [InlineData("BindAsyncFromStaticAbstractInterfaceWrongType")]
    [InlineData("InheritBindAsyncWrongType")]
    public async Task MapAction_BindAsync_WithWrongType_IsNotUsed(string bindAsyncType)
    {
        var source = $$"""
app.MapGet("/", ({{bindAsyncType}} myNotBindAsyncParam) => { });
""";
        var (result, compilation) = await RunGeneratorAsync(source);

        var ex = Assert.Throws<InvalidOperationException>(() => GetEndpointFromCompilation(compilation));
        Assert.StartsWith($"BindAsync method found on {bindAsyncType} with incorrect format.", ex.Message);
    }

    // TODO: https://github.com/dotnet/aspnetcore/issues/47200
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateLogsIOExceptionsForFormAsDebugDoesNotAbortAndNeverThrows(bool throwOnBadRequests)
    {
        var source = """
void TestAction(HttpContext httpContext, IFormFile file)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = throwOnBadRequests);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var ioException = new IOException();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(ioException);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(1, "RequestBodyIOException"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal("Reading the request body failed with an IOException.", logMessage.Message);
        Assert.Same(ioException, logMessage.Exception);
    }

    [Fact]
    public async Task RequestDelegateLogsMalformedFormAsDebugAndSets400Response()
    {
        var source = """
void TestAction(HttpContext httpContext, IFormFile file)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider();
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "2049";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', 2049)));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(8, "InvalidFormRequestBody"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal(@"Failed to read parameter ""IFormFile file"" from the request body as form.", logMessage.Message);
        Assert.IsType<InvalidDataException>(logMessage.Exception);
    }

    [Fact]
    public async Task RequestDelegateThrowsForMalformedFormIfThrowOnBadRequest()
    {
        var source = """
void TestAction(HttpContext httpContext, IFormFile file)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "2049";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', 2049)));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));

        Assert.Null(httpContext.Items["invoked"]);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to read parameter ""IFormFile file"" from the request body as form.", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
        Assert.IsType<InvalidDataException>(badHttpRequestException.InnerException);
    }
}
