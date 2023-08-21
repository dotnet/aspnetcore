// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests
{
    public static object[][] MapAction_ExplicitBodyParam_ComplexReturn_Data
    {
        get
        {
            var expectedBody = """{"id":0,"name":"Test Item","isComplete":false}""";
            var todo = new Todo()
            {
                Id = 0,
                Name = "Test Item",
                IsComplete = false
            };
            var withFilter = """
.AddEndpointFilter((c, n) => n(c));
""";
            var fromBodyRequiredSource = """app.MapPost("/", ([FromBody] Todo todo) => TypedResults.Ok(todo));""";
            var fromBodyEmptyBodyBehaviorSource = """app.MapPost("/", ([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Todo todo) => TypedResults.Ok(todo));""";
            var fromBodyAllowEmptySource = """app.MapPost("/", ([CustomFromBody(AllowEmpty = true)] Todo todo) => TypedResults.Ok(todo));""";
            var fromBodyNullableSource = """app.MapPost("/", ([FromBody] Todo? todo) => TypedResults.Ok(todo));""";
            var fromBodyDefaultValueSource = """
#nullable disable
IResult postTodoWithDefault([FromBody] Todo todo = null) => TypedResults.Ok(todo);
app.MapPost("/", postTodoWithDefault);
#nullable restore
""";
            var fromBodyAsParametersRequiredSource = """app.MapPost("/", ([AsParameters] ParametersListWithExplicitFromBody args) => TypedResults.Ok(args.Todo));""";
            var fromBodyRequiredWithFilterSource = $"""app.MapPost("/", ([FromBody] Todo todo) => TypedResults.Ok(todo)){withFilter}""";
            var fromBodyEmptyBehaviorWithFilterSource = $"""app.MapPost("/", ([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Todo todo) => TypedResults.Ok(todo)){withFilter}""";
            var fromBodyAllowEmptyWithFilterSource = $"""app.MapPost("/", ([CustomFromBody(AllowEmpty = true)] Todo todo) => TypedResults.Ok(todo)){withFilter}""";
            var fromBodyNullableWithFilterSource = $"""app.MapPost("/", ([FromBody] Todo?  todo) => TypedResults.Ok(todo)){withFilter}""";
            var fromBodyDefaultValueWithFilterSource = $"""
#nullable disable
IResult postTodoWithDefault([FromBody] Todo todo = null) => TypedResults.Ok(todo);
app.MapPost("/", postTodoWithDefault){withFilter}
#nullable restore
""";

            return new[]
            {
                new object[] { fromBodyRequiredSource, todo, 200, expectedBody },
                new object[] { fromBodyRequiredSource, null, 400, string.Empty },
                new object[] { fromBodyAsParametersRequiredSource, todo, 200, expectedBody },
                new object[] { fromBodyEmptyBodyBehaviorSource, todo, 200, expectedBody },
                new object[] { fromBodyEmptyBodyBehaviorSource, null, 200, string.Empty },
                new object[] { fromBodyAllowEmptySource, todo, 200, expectedBody },
                new object[] { fromBodyAllowEmptySource, null, 200, string.Empty },
                new object[] { fromBodyNullableSource, todo, 200, expectedBody },
                new object[] { fromBodyNullableSource, null, 200, string.Empty },
                new object[] { fromBodyDefaultValueSource, todo, 200, expectedBody },
                new object[] { fromBodyDefaultValueSource, null, 200, string.Empty },
                new object[] { fromBodyRequiredWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyRequiredWithFilterSource, null, 400, string.Empty },
                new object[] { fromBodyEmptyBehaviorWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyEmptyBehaviorWithFilterSource, null, 200, string.Empty },
                new object[] { fromBodyAllowEmptyWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyAllowEmptyWithFilterSource, null, 200, string.Empty },
                new object[] { fromBodyNullableWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyNullableWithFilterSource, null, 200, string.Empty },
                new object[] { fromBodyDefaultValueWithFilterSource, todo, 200, expectedBody },
                new object[] { fromBodyDefaultValueSource, null, 200, string.Empty },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_ExplicitBodyParam_ComplexReturn_Data))]
    public async Task MapAction_ExplicitBodyParam_ComplexReturn(string source, Todo requestData, int expectedStatusCode, string expectedBody)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(requestData is not null));
        httpContext.Request.Headers["Content-Type"] = "application/json";

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(requestData);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody, expectedStatusCode);
    }

    [Fact]
    public async Task MapAction_ExplicitBodyParam_ComplexReturn_Returns400ForEmptyBody()
    {
        var source = """app.MapPost("/", ([FromBody] Todo todo) => TypedResults.Ok(todo));""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(false));
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "0";

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, string.Empty, expectedStatusCode: 400);
    }

    [Fact]
    public async Task MapAction_ExplicitBodyParam_ComplexReturn_Snapshot()
    {
        var expectedBody = """{"id":0,"name":"Test Item","isComplete":false}""";
        var todo = new Todo()
        {
            Id = 0,
            Name = "Test Item",
            IsComplete = false
        };
        var source = $"""
app.MapPost("/fromBodyRequired", ([FromBody] Todo todo) => TypedResults.Ok(todo));
app.MapPost("/fromBodyOptional", ([FromBody] Todo? todo) => TypedResults.Ok(todo));
""";
        var (_, compilation) = await RunGeneratorAsync(source);

        await VerifyAgainstBaselineUsingFile(compilation);

        var endpoints = GetEndpointsFromCompilation(compilation);

        Assert.Equal(2, endpoints.Length);

        // formBodyRequired accepts a provided input
        var httpContext = CreateHttpContextWithBody(todo);
        await endpoints[0].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);

        // formBodyRequired throws on null input
        httpContext = CreateHttpContextWithBody(null);
        await endpoints[0].RequestDelegate(httpContext);
        Assert.Equal(400, httpContext.Response.StatusCode);

        // formBodyOptional accepts a provided input
        httpContext = CreateHttpContextWithBody(todo);
        await endpoints[1].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);

        // formBodyOptional accepts a null input
        httpContext = CreateHttpContextWithBody(null);
        await endpoints[1].RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, string.Empty);
    }

    public static object[][] ImplicitRawFromBodyActions
    {
        get
        {
            var testStreamSource = """
void TestStream(HttpContext httpContext, System.IO.Stream stream)
{
    var ms = new System.IO.MemoryStream();
    stream.CopyTo(ms);
    httpContext.Items.Add("body", ms.ToArray());
}
app.MapPost("/", TestStream);
""";
            var testPipeReaderSource = """
async Task TestPipeReader(HttpContext httpContext, System.IO.Pipelines.PipeReader reader)
{
    var ms = new System.IO.MemoryStream();
    await reader.CopyToAsync(ms);
    httpContext.Items.Add("body", ms.ToArray());
}
app.MapPost("/", TestPipeReader);
""";

            return new[]
            {
                new object[] { testStreamSource },
                new object[] { testPipeReaderSource }
            };
        }
    }

    public static object[][] ExplicitRawFromBodyActions
    {
        get
        {
            var explicitTestStreamSource = """
void TestStream(HttpContext httpContext, [FromBody] System.IO.Stream stream)
{
    var ms = new System.IO.MemoryStream();
    stream.CopyTo(ms);
    httpContext.Items.Add("body", ms.ToArray());
}
app.MapPost("/", TestStream);
""";

            var explicitTestPipeReaderSource = """
async Task TestPipeReader(HttpContext httpContext, [FromBody] System.IO.Pipelines.PipeReader reader)
{
    var ms = new System.IO.MemoryStream();
    await reader.CopyToAsync(ms);
    httpContext.Items.Add("body", ms.ToArray());
}
app.MapPost("/", TestPipeReader);
""";

            return new[]
            {
                new object[] { explicitTestStreamSource },
                new object[] { explicitTestPipeReaderSource }
            };
        }
    }

    [Theory]
    [MemberData(nameof(ImplicitRawFromBodyActions))]
    public async Task RequestDelegatePopulatesFromImplicitRawBodyParameter(string source)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Name = "Write more tests!"
        });

        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Same(httpContext.Request.Body, stream);

        // Assert that we can read the body from both the pipe reader and Stream after executing
        httpContext.Request.Body.Position = 0;
        byte[] data = new byte[requestBodyBytes.Length];
        int read = await httpContext.Request.Body.ReadAsync(data.AsMemory());
        Assert.Equal(read, data.Length);
        Assert.Equal(requestBodyBytes, data);

        httpContext.Request.Body.Position = 0;
        var result = await httpContext.Request.BodyReader.ReadAsync();
        Assert.Equal(requestBodyBytes.Length, result.Buffer.Length);
        Assert.Equal(requestBodyBytes, result.Buffer.ToArray());
        httpContext.Request.BodyReader.AdvanceTo(result.Buffer.End);

        var rawRequestBody = httpContext.Items["body"];
        Assert.NotNull(rawRequestBody);
        Assert.Equal(requestBodyBytes, (byte[])rawRequestBody!);
    }

    [Theory]
    [MemberData(nameof(ExplicitRawFromBodyActions))]
    public async Task RequestDelegatePopulatesFromExplicitRawBodyParameter(string source)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Name = "Write more tests!"
        });

        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Same(httpContext.Request.Body, stream);

        // Assert that we can read the body from both the pipe reader and Stream after executing
        httpContext.Request.Body.Position = 0;
        byte[] data = new byte[requestBodyBytes.Length];
        int read = await httpContext.Request.Body.ReadAsync(data.AsMemory());
        Assert.Equal(read, data.Length);
        Assert.Equal(requestBodyBytes, data);

        httpContext.Request.Body.Position = 0;
        var result = await httpContext.Request.BodyReader.ReadAsync();
        Assert.Equal(requestBodyBytes.Length, result.Buffer.Length);
        Assert.Equal(requestBodyBytes, result.Buffer.ToArray());
        httpContext.Request.BodyReader.AdvanceTo(result.Buffer.End);

        var rawRequestBody = httpContext.Items["body"];
        Assert.NotNull(rawRequestBody);
        Assert.Equal(requestBodyBytes, (byte[])rawRequestBody!);
    }

    [Theory]
    [MemberData(nameof(ImplicitRawFromBodyActions))]
    public async Task RequestDelegatePopulatesFromImplicitRawBodyParameterPipeReader(string source)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Name = "Write more tests!"
        });

        var pipeReader = PipeReader.Create(new MemoryStream(requestBodyBytes));
        var stream = pipeReader.AsStream();
        httpContext.Features.Set<IRequestBodyPipeFeature>(new PipeRequestBodyFeature(pipeReader));
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Length"] = requestBodyBytes.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Same(httpContext.Request.Body, stream);
        Assert.Same(httpContext.Request.BodyReader, pipeReader);

        // Assert that we can read the body from both the pipe reader and Stream after executing and verify that they are empty (the pipe reader isn't seekable here)
        int read = await httpContext.Request.Body.ReadAsync(new byte[requestBodyBytes.Length].AsMemory());
        Assert.Equal(0, read);

        var result = await httpContext.Request.BodyReader.ReadAsync();
        Assert.Equal(0, result.Buffer.Length);
        Assert.True(result.IsCompleted);
        httpContext.Request.BodyReader.AdvanceTo(result.Buffer.End);

        var rawRequestBody = httpContext.Items["body"];
        Assert.NotNull(rawRequestBody);
        Assert.Equal(requestBodyBytes, (byte[])rawRequestBody!);
    }

    [Theory]
    [MemberData(nameof(ExplicitRawFromBodyActions))]
    public async Task RequestDelegatePopulatesFromExplicitRawBodyParameterPipeReader(string source)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Name = "Write more tests!"
        });

        var pipeReader = PipeReader.Create(new MemoryStream(requestBodyBytes));
        var stream = pipeReader.AsStream();
        httpContext.Features.Set<IRequestBodyPipeFeature>(new PipeRequestBodyFeature(pipeReader));
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Length"] = requestBodyBytes.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        await endpoint.RequestDelegate(httpContext);

        Assert.Same(httpContext.Request.Body, stream);
        Assert.Same(httpContext.Request.BodyReader, pipeReader);

        // Assert that we can read the body from both the pipe reader and Stream after executing and verify that they are empty (the pipe reader isn't seekable here)
        int read = await httpContext.Request.Body.ReadAsync(new byte[requestBodyBytes.Length].AsMemory());
        Assert.Equal(0, read);

        var result = await httpContext.Request.BodyReader.ReadAsync();
        Assert.Equal(0, result.Buffer.Length);
        Assert.True(result.IsCompleted);
        httpContext.Request.BodyReader.AdvanceTo(result.Buffer.End);

        var rawRequestBody = httpContext.Items["body"];
        Assert.NotNull(rawRequestBody);
        Assert.Equal(requestBodyBytes, (byte[])rawRequestBody!);
    }

    [Fact]
    public async Task RequestDelegateAllowsEmptyBodyStructGivenCorrectlyConfiguredFromBodyParameter()
    {
        var structToBeZeroedKey = "structToBeZeroed";

        var source = $$"""
void TestAction(HttpContext httpContext, [CustomFromBody(AllowEmpty = true)] BodyStruct bodyStruct)
{
    httpContext.Items["{{structToBeZeroedKey}}"] = bodyStruct;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "0";
        httpContext.Items[structToBeZeroedKey] = new BodyStruct { Id = 42 };

        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(default(BodyStruct), httpContext.Items[structToBeZeroedKey]);
    }

    [Fact]
    public async Task RequestDelegateHandlesRequiredBodyStruct()
    {
        var targetStruct = new BodyStruct
        {
            Id = 42
        };

        var source = $$"""
void TestAction(HttpContext httpContext, BodyStruct bodyStruct)
{
    httpContext.Items["targetStruct"] = bodyStruct;
}
app.MapPost("/", TestAction);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.Request.Headers["Content-Type"] = "application/json";

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(targetStruct);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);

        await endpoint.RequestDelegate(httpContext);

        var resultStruct = Assert.IsType<BodyStruct>(httpContext.Items["targetStruct"]);
        Assert.Equal(42, resultStruct.Id);
    }

    public static IEnumerable<object[]> AllowEmptyData
    {
        get
        {
            return new List<object[]>
                {
                    new object[] { $@"string handler([CustomFromBody(AllowEmpty = false)] Todo todo) => todo?.ToString() ?? string.Empty", false },
                    new object[] { $@"string handler([CustomFromBody(AllowEmpty = true)] Todo todo) => todo?.ToString() ?? string.Empty", true },
                    new object[] { $@"string handler([CustomFromBody(AllowEmpty = true)] Todo? todo = null) => todo?.ToString() ?? string.Empty", true },
                    new object[] { $@"string handler([CustomFromBody(AllowEmpty = false)] Todo? todo = null) => todo?.ToString() ?? string.Empty", true }
                };
        }
    }

    [Theory]
    [MemberData(nameof(AllowEmptyData))]
    public async Task AllowEmptyOverridesOptionality(string innerSource, bool allowsEmptyRequest)
    {
        var source = $"""
{innerSource};
app.MapPost("/", handler);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContextWithBody(null);

        await endpoint.RequestDelegate(httpContext);

        var logs = TestSink.Writes.ToArray();

        if (!allowsEmptyRequest)
        {
            Assert.Equal(400, httpContext.Response.StatusCode);
            var log = Assert.Single(logs);
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
            Assert.Equal(@"Required parameter ""Todo todo"" was not provided from body.", log.Message);
        }
        else
        {
            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        }
    }

}
