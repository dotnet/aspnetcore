// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
{
    [Fact]
    public async Task RequestDelegateLogsStringValuesFromExplicitQueryStringSourceForUnpresentedValuesFailuresAsDebugAndSets400Response()
    {
        var source = """
app.MapGet("/{baz}", (
    HttpContext httpContext,
    [FromHeader(Name = "foo")] StringValues headerValues,
    [FromQuery(Name = "bar")] StringValues queryValues,
    [FromForm(Name = "form")] StringValues formValues,
    [FromRoute(Name = "baz")] string routeValues
) =>
{
    httpContext.Items["invoked"] = true;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider();
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext(serviceProvider);
        httpContext.Request.Form = new FormCollection(null);

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logs = TestSink.Writes.ToArray();

        Assert.Equal(4, logs.Length);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[0].EventId);
        Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
        Assert.Equal(@"Required parameter ""StringValues headerValues"" was not provided from header.", logs[0].Message);
        var log1Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[0].State);
        Assert.Equal("StringValues", log1Values[0].Value);
        Assert.Equal("headerValues", log1Values[1].Value);
        Assert.Equal("header", log1Values[2].Value);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[1].EventId);
        Assert.Equal(LogLevel.Debug, logs[1].LogLevel);
        Assert.Equal(@"Required parameter ""StringValues queryValues"" was not provided from query string.", logs[1].Message);
        var log2Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[1].State);
        Assert.Equal("StringValues", log2Values[0].Value);
        Assert.Equal("queryValues", log2Values[1].Value);
        Assert.Equal("query string", log2Values[2].Value);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[2].EventId);
        Assert.Equal(LogLevel.Debug, logs[2].LogLevel);
        Assert.Equal(@"Required parameter ""StringValues formValues"" was not provided from form.", logs[2].Message);
        var log3Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[2].State);
        Assert.Equal("StringValues", log3Values[0].Value);
        Assert.Equal("formValues", log3Values[1].Value);
        Assert.Equal("form", log3Values[2].Value);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[3].EventId);
        Assert.Equal(LogLevel.Debug, logs[3].LogLevel);
        Assert.Equal(@"Required parameter ""string routeValues"" was not provided from route.", logs[3].Message);
        var log4Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[3].State);
        Assert.Equal("string", log4Values[0].Value);
        Assert.Equal("routeValues", log4Values[1].Value);
        Assert.Equal("route", log4Values[2].Value);
    }

    [Fact]
    public async Task RequestDelegateLogsTryParsableFailuresAsDebugAndSets400Response()
    {
        var source = """
void TestAction(HttpContext httpContext, [FromRoute] int tryParsable, [FromRoute] int tryParsable2)
{
    httpContext.Items["invoked"] = true;
}

app.MapGet("/{tryParsable}/{tryParsable2}", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider();
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["tryParsable"] = "invalid!";
        httpContext.Request.RouteValues["tryParsable2"] = "invalid again!";

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logs = TestSink.Writes.ToArray();

        Assert.Equal(2, logs.Length);

        Assert.Equal(new EventId(3, "ParameterBindingFailed"), logs[0].EventId);
        Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
        Assert.Equal(@"Failed to bind parameter ""int tryParsable"" from ""invalid!"".", logs[0].Message);
        var log1Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[0].State);
        Assert.Equal("int", log1Values[0].Value);
        Assert.Equal("tryParsable", log1Values[1].Value);
        Assert.Equal("invalid!", log1Values[2].Value);

        Assert.Equal(new EventId(3, "ParameterBindingFailed"), logs[1].EventId);
        Assert.Equal(LogLevel.Debug, logs[1].LogLevel);
        Assert.Equal(@"Failed to bind parameter ""int tryParsable2"" from ""invalid again!"".", logs[1].Message);
        var log2Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[1].State);
        Assert.Equal("int", log2Values[0].Value);
        Assert.Equal("tryParsable2", log2Values[1].Value);
        Assert.Equal("invalid again!", log2Values[2].Value);

    }

    [Fact]
    public async Task RequestDelegateThrowsForTryParsableFailuresIfThrowOnBadRequest()
    {
        var source = """
void TestAction(HttpContext httpContext, [FromRoute] int tryParsable, [FromRoute] int tryParsable2)
{
    httpContext.Items["invoked"] = true;
}

app.MapGet("/{tryParsable}/{tryParsable2}", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["tryParsable"] = "invalid!";
        httpContext.Request.RouteValues["tryParsable2"] = "invalid again!";

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));

        Assert.Null(httpContext.Items["invoked"]);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to bind parameter ""int tryParsable"" from ""invalid!"".", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    [Fact]
    public async Task RequestDelegateThrowsForTryParsableFailuresIfThrowOnBadRequestWithArrays()
    {
        var source = """
void TestAction(HttpContext httpContext, [FromQuery] int[] values)
{
    httpContext.Items["invoked"] = true;
}
app.MapGet("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>()
        {
            ["values"] = new(new[] { "1", "NAN", "3" })
        });

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to bind parameter ""int[] values"" from ""NAN"".", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    [Fact]
    public async Task RequestDelegateLogsBindAsyncFailuresAndSets400Response()
    {
        var source = """
void TestAction(HttpContext httpContext, MyBindAsyncRecord myBindAsyncParam1, MyBindAsyncRecord myBindAsyncParam2)
{
    httpContext.Items["invoked"] = true;
}
app.MapGet("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider();
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        // Not supplying any headers will cause the HttpContext BindAsync overload to return null.
        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);

        var logs = TestSink.Writes.ToArray();

        Assert.Equal(2, logs.Length);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[0].EventId);
        Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
        Assert.Equal(@"Required parameter ""MyBindAsyncRecord myBindAsyncParam1"" was not provided from MyBindAsyncRecord.BindAsync(HttpContext, ParameterInfo).", logs[0].Message);
        var log1Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[0].State);
        Assert.Equal("MyBindAsyncRecord", log1Values[0].Value);
        Assert.Equal("myBindAsyncParam1", log1Values[1].Value);
        Assert.Equal("MyBindAsyncRecord.BindAsync(HttpContext, ParameterInfo)", log1Values[2].Value);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[1].EventId);
        Assert.Equal(LogLevel.Debug, logs[1].LogLevel);
        Assert.Equal(@"Required parameter ""MyBindAsyncRecord myBindAsyncParam2"" was not provided from MyBindAsyncRecord.BindAsync(HttpContext, ParameterInfo).", logs[1].Message);
        var log2Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[1].State);
        Assert.Equal("MyBindAsyncRecord", log2Values[0].Value);
        Assert.Equal("myBindAsyncParam2", log2Values[1].Value);
        Assert.Equal("MyBindAsyncRecord.BindAsync(HttpContext, ParameterInfo)", log2Values[2].Value);
    }

    [Fact]
    public async Task RequestDelegateThrowsForBindAsyncFailuresIfThrowOnBadRequest()
    {
        var source = """
void TestAction(HttpContext httpContext, MyBindAsyncRecord myBindAsyncParam1, MyBindAsyncRecord myBindAsyncParam2)
{
    httpContext.Items["invoked"] = true;
}
app.MapGet("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        // Not supplying any headers will cause the HttpContext BindAsync overload to return null.
        var httpContext = CreateHttpContext();

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));

        Assert.Null(httpContext.Items["invoked"]);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Required parameter ""MyBindAsyncRecord myBindAsyncParam1"" was not provided from MyBindAsyncRecord.BindAsync(HttpContext, ParameterInfo).", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    [Fact]
    public async Task RequestDelegateLogsSingleArgBindAsyncFailuresAndSets400Response()
    {
        var source = """
void TestAction(HttpContext httpContext, MySimpleBindAsyncRecord mySimpleBindAsyncRecord1, MySimpleBindAsyncRecord mySimpleBindAsyncRecord2)
{
    httpContext.Items["invoked"] = true;
}
app.MapGet("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider();
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        // Not supplying any headers will cause the HttpContext BindAsync overload to return null.
        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);

        var logs = TestSink.Writes.ToArray();

        Assert.Equal(2, logs.Length);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[0].EventId);
        Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
        Assert.Equal(@"Required parameter ""MySimpleBindAsyncRecord mySimpleBindAsyncRecord1"" was not provided from MySimpleBindAsyncRecord.BindAsync(HttpContext).", logs[0].Message);
        var log1Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[0].State);
        Assert.Equal("MySimpleBindAsyncRecord", log1Values[0].Value);
        Assert.Equal("mySimpleBindAsyncRecord1", log1Values[1].Value);
        Assert.Equal("MySimpleBindAsyncRecord.BindAsync(HttpContext)", log1Values[2].Value);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[1].EventId);
        Assert.Equal(LogLevel.Debug, logs[1].LogLevel);
        Assert.Equal(@"Required parameter ""MySimpleBindAsyncRecord mySimpleBindAsyncRecord2"" was not provided from MySimpleBindAsyncRecord.BindAsync(HttpContext).", logs[1].Message);
        var log2Values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logs[1].State);
        Assert.Equal("MySimpleBindAsyncRecord", log2Values[0].Value);
        Assert.Equal("mySimpleBindAsyncRecord2", log2Values[1].Value);
        Assert.Equal("MySimpleBindAsyncRecord.BindAsync(HttpContext)", log2Values[2].Value);
    }

    [Fact]
    public async Task RequestDelegateThrowsForSingleArgBindAsyncFailuresIfThrowOnBadRequest()
    {
        var source = """
void TestAction(HttpContext httpContext, MySimpleBindAsyncRecord mySimpleBindAsyncRecord1, MySimpleBindAsyncRecord mySimpleBindAsyncRecord2)
{
    httpContext.Items["invoked"] = true;
}
app.MapGet("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        // Not supplying any headers will cause the HttpContext BindAsync overload to return null.
        var httpContext = CreateHttpContext();
        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));

        Assert.Null(httpContext.Items["invoked"]);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Required parameter ""MySimpleBindAsyncRecord mySimpleBindAsyncRecord1"" was not provided from MySimpleBindAsyncRecord.BindAsync(HttpContext).", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateRejectsNonJsonContent(bool shouldThrow)
    {
        var source = """
void TestAction(HttpContext httpContext, Todo todo)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = shouldThrow);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/xml";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var request = endpoint.RequestDelegate(httpContext);

        if (shouldThrow)
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => request);
            Assert.Equal("Expected a supported JSON media type but got \"application/xml\".", ex.Message);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, ex.StatusCode);
        }
        else
        {
            await request;

            Assert.Equal(415, httpContext.Response.StatusCode);
            var logMessage = Assert.Single(TestSink.Writes);
            Assert.Equal(new EventId(6, "UnexpectedContentType"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Equal("Expected a supported JSON media type but got \"application/xml\".", logMessage.Message);
            var logValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logMessage.State);
            Assert.Equal("application/xml", logValues[0].Value);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateWithBindAndImplicitBodyRejectsNonJsonContent(bool shouldThrow)
    {
        var source = """
void TestAction(HttpContext httpContext, Todo todo)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = shouldThrow);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        Todo originalTodo = new()
        {
            Name = "Write more tests!"
        };

        var httpContext = CreateHttpContextWithBody(originalTodo);
        httpContext.Request.Headers["Content-Type"] = "application/xml";

        var request = endpoint.RequestDelegate(httpContext);

        if (shouldThrow)
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => request);
            Assert.Equal("Expected a supported JSON media type but got \"application/xml\".", ex.Message);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, ex.StatusCode);
        }
        else
        {
            await request;

            Assert.Equal(415, httpContext.Response.StatusCode);
            var logMessage = Assert.Single(TestSink.Writes);
            Assert.Equal(new EventId(6, "UnexpectedContentType"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Equal("Expected a supported JSON media type but got \"application/xml\".", logMessage.Message);
            var logValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logMessage.State);
            Assert.Equal("application/xml", logValues[0].Value);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateLogsIOExceptionsAsDebugDoesNotAbortAndNeverThrows(bool throwOnBadRequests)
    {
        var source = """
void TestAction(HttpContext httpContext, [FromBody] Todo todo)
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
        httpContext.Request.Headers["Content-Type"] = "application/json";
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
    public async Task RequestDelegateLogsJsonExceptionsAsDebugAndSets400Response()
    {
        var source = """
void TestAction(HttpContext httpContext, [FromBody] Todo todo)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);
        var jsonException = new JsonException();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(jsonException);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(2, "InvalidJsonRequestBody"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal(@"Failed to read parameter ""Todo todo"" from the request body as JSON.", logMessage.Message);
        Assert.Same(jsonException, logMessage.Exception);
        var logValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logMessage.State);
        Assert.Equal("Todo", logValues[0].Value);
        Assert.Equal("todo", logValues[1].Value);
    }

    [Fact]
    public async Task RequestDelegateThrowsForJsonExceptionsIfThrowOnBadRequest()
    {
        var source = """
void TestAction(HttpContext httpContext, [FromBody] Todo todo)
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
        var jsonException = new JsonException();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(jsonException);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));

        Assert.Null(httpContext.Items["invoked"]);
        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to read parameter ""Todo todo"" from the request body as JSON.", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
        Assert.Same(jsonException, badHttpRequestException.InnerException);
    }

    [Fact]
    public async Task RequestDelegateLogsMalformedJsonAsDebugAndSets400Response()
    {
        var source = """
void TestAction(HttpContext httpContext, [FromBody] Todo todo)
{
    httpContext.Items["invoked"] = true;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(serviceCollection =>
        {
            serviceCollection.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{"));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Null(httpContext.Items["invoked"]);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(2, "InvalidJsonRequestBody"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal(@"Failed to read parameter ""Todo todo"" from the request body as JSON.", logMessage.Message);
        Assert.IsType<JsonException>(logMessage.Exception);
        var logValues = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object>>>(logMessage.State);
        Assert.Equal("Todo", logValues[0].Value);
        Assert.Equal("todo", logValues[1].Value);
    }

    [Fact]
    public async Task RequestDelegateThrowsForMalformedJsonIfThrowOnBadRequest()
    {
        var source = """
void TestAction(HttpContext httpContext, [FromBody] Todo todo)
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
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{"));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate(httpContext));

        Assert.Null(httpContext.Items["invoked"]);
        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to read parameter ""Todo todo"" from the request body as JSON.", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
        Assert.IsType<JsonException>(badHttpRequestException.InnerException);
    }
}
