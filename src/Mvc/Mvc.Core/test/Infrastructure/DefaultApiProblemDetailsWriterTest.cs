// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class DefaultApiProblemDetailsWriterTest
{

    [Fact]
    public async Task WriteAsync_Works()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);

        var expectedProblem = new ProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
        };
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, JsonSerializerOptions.Web);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
    }

    [Fact]
    public async Task WriteAsync_Works_WhenReplacingProblemDetailsUsingSetter()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var originalProblemDetails = new ProblemDetails();

        var expectedProblem = new ProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1-custom",
            Title = "Custom Bad Request",
        };
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = originalProblemDetails
        };

        problemDetailsContext.ProblemDetails = expectedProblem;

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, JsonSerializerOptions.Web);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedProblem.Status, problemDetails.Status);
        Assert.Equal(expectedProblem.Type, problemDetails.Type);
        Assert.Equal(expectedProblem.Title, problemDetails.Title);
        Assert.Equal(expectedProblem.Detail, problemDetails.Detail);
        Assert.Equal(expectedProblem.Instance, problemDetails.Instance);
    }

    [Fact]
    public async Task WriteAsync_AddExtensions()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);
        var expectedProblem = new ProblemDetails();
        expectedProblem.Extensions["Extension1"] = "Extension1-Value";
        expectedProblem.Extensions["Extension2"] = "Extension2-Value";

        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream);
        Assert.NotNull(problemDetails);
        Assert.Contains("Extension1", problemDetails.Extensions);
        Assert.Contains("Extension2", problemDetails.Extensions);
    }

    [Fact]
    public void CanWrite_ReturnsFalse_WhenNoEndpointMetadata()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream, metadata: EndpointMetadataCollection.Empty);

        //Act
        var result = writer.CanWrite(new() { HttpContext = context });

        //Assert
        Assert.False(result);
    }

    [Fact]
    public void CanWrite_ReturnsFalse_WhenControllerWithoutApiControllerAttribute()
    {
        // Arrange - controller endpoint without [ApiController]; the writer
        // would silently drop the body, so CanWrite must return false to let
        // another IProblemDetailsWriter handle it.
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream, metadata: new EndpointMetadataCollection(new ControllerAttribute()));

        //Act
        var result = writer.CanWrite(new() { HttpContext = context });

        //Assert
        Assert.False(result);
    }

    [Fact]
    public void CanWrite_ReturnsTrue_WhenApiController()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream, metadata: new EndpointMetadataCollection(new ApiControllerAttribute(), new ControllerAttribute()));

        //Act
        var result = writer.CanWrite(new() { HttpContext = context });

        //Assert
        Assert.True(result);
    }

    [Fact]
    public void CanWrite_ReturnsFalse_WhenSuppressMapClientErrors()
    {
        // Arrange
        var writer = GetWriter(options: new ApiBehaviorOptions() { SuppressMapClientErrors = true });
        var stream = new MemoryStream();
        var context = CreateContext(stream);

        //Act
        var result = writer.CanWrite(new() { HttpContext = context });

        //Assert
        Assert.False(result);
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenNotApiController()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream, metadata: new EndpointMetadataCollection(new ControllerAttribute()));

        //Act
        await writer.WriteAsync(new() { HttpContext = context });

        //Assert
        Assert.Equal(0, stream.Position);
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public async Task WriteAsync_PreservesValidationProblemDetailsErrors()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream);

        var expectedErrors = new Dictionary<string, string[]>
        {
            ["field"] = new[] { "is required" },
        };
        var expectedProblem = new ValidationProblemDetails(expectedErrors)
        {
            Detail = "Validation failed",
            Instance = "/repro",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://example.com/probs/validation",
            Title = "One or more validation errors occurred.",
        };
        expectedProblem.Extensions["customField"] = "demonstrates extension preservation";

        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem,
        };

        //Act
        await writer.WriteAsync(problemDetailsContext);

        //Assert
        stream.Position = 0;
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        Assert.Equal(expectedProblem.Status, root.GetProperty("status").GetInt32());
        Assert.Equal(expectedProblem.Type, root.GetProperty("type").GetString());
        Assert.Equal(expectedProblem.Title, root.GetProperty("title").GetString());
        Assert.Equal(expectedProblem.Detail, root.GetProperty("detail").GetString());
        Assert.Equal(expectedProblem.Instance, root.GetProperty("instance").GetString());

        var errors = root.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        var fieldErrors = errors.GetProperty("field");
        Assert.Equal(JsonValueKind.Array, fieldErrors.ValueKind);
        Assert.Equal("is required", fieldErrors[0].GetString());

        Assert.Equal("demonstrates extension preservation", root.GetProperty("customField").GetString());
    }

    [Fact]
    public async Task WriteAsync_PassesValidationProblemDetailsRuntimeTypeToFormatter()
    {
        // Arrange - the formatter context's ObjectType drives XML/content-negotiation
        // formatter selection (e.g. the validation problem details wrapper provider only
        // matches when the declared type is ValidationProblemDetails). Capture the type.
        var capturingFormatter = new CapturingFormatter();
        var writer = GetWriter(formatter: capturingFormatter);
        var stream = new MemoryStream();
        var context = CreateContext(stream);

        var expectedProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["field"] = new[] { "is required" },
        });

        //Act
        await writer.WriteAsync(new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = expectedProblem,
        });

        //Assert
        Assert.NotNull(capturingFormatter.LastContext);
        Assert.Equal(typeof(ValidationProblemDetails), capturingFormatter.LastContext.ObjectType);
        Assert.IsType<ValidationProblemDetails>(capturingFormatter.LastContext.Object);
    }

    [Fact]
    public async Task WriterChain_FallsThroughToNextWriter_ForControllerWithoutApiControllerAttribute()
    {
        // Arrange - regression for https://github.com/dotnet/aspnetcore/issues/67053:
        // a plain controller endpoint (no [ApiController]) must not be claimed and then
        // silently dropped by DefaultApiProblemDetailsWriter. Simulate the writer-selection
        // loop performed by ProblemDetailsService and verify the next writer runs.
        var apiWriter = GetWriter();
        var fallbackWriter = new RecordingWriter();
        var writers = new IProblemDetailsWriter[] { apiWriter, fallbackWriter };

        var stream = new MemoryStream();
        var context = CreateContext(
            stream,
            metadata: new EndpointMetadataCollection(new ControllerAttribute()));
        var problemDetailsContext = new ProblemDetailsContext()
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = StatusCodes.Status404NotFound },
        };

        //Act - mirror ProblemDetailsService.TryWriteAsync
        var written = false;
        foreach (var writer in writers)
        {
            if (writer.CanWrite(problemDetailsContext))
            {
                await writer.WriteAsync(problemDetailsContext);
                written = true;
                break;
            }
        }

        //Assert
        Assert.True(written);
        Assert.True(fallbackWriter.WriteAsyncCalled);
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenSuppressMapClientErrors()
    {
        // Arrange
        var writer = GetWriter(options: new ApiBehaviorOptions() { SuppressMapClientErrors = true });
        var stream = new MemoryStream();
        var context = CreateContext(stream);

        //Act
        await writer.WriteAsync(new() { HttpContext = context });

        //Assert
        Assert.Equal(0, stream.Position);
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenNoFormatter()
    {
        // Arrange
        var formatter = new Mock<IOutputFormatter>();
        formatter.Setup(f => f.CanWriteResult(It.IsAny<OutputFormatterWriteContext>())).Returns(false);
        var writer = GetWriter(formatter: formatter.Object);
        var stream = new MemoryStream();
        var context = CreateContext(stream);

        //Act
        await writer.WriteAsync(new() { HttpContext = context });

        //Assert
        Assert.Equal(0, stream.Position);
        Assert.Equal(0, stream.Length);
    }

    private static HttpContext CreateContext(Stream body, int statusCode = StatusCodes.Status400BadRequest, EndpointMetadataCollection metadata = null)
    {
        metadata ??= new EndpointMetadataCollection(new ApiControllerAttribute(), new ControllerAttribute());

        var context = new DefaultHttpContext()
        {
            Response = { Body = body, StatusCode = statusCode },
            RequestServices = CreateServices()
        };
        context.SetEndpoint(new Endpoint(null, metadata, string.Empty));

        return context;
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return services.BuildServiceProvider();
    }

    private static DefaultApiProblemDetailsWriter GetWriter(ApiBehaviorOptions options = null, IOutputFormatter formatter = null)
    {
        options ??= new ApiBehaviorOptions();
        formatter ??= new TestFormatter();

        var mvcOptions = Options.Create(new MvcOptions());
        mvcOptions.Value.OutputFormatters.Add(formatter);

        return new DefaultApiProblemDetailsWriter(
            new DefaultOutputFormatterSelector(mvcOptions, NullLoggerFactory.Instance),
            new TestHttpResponseStreamWriterFactory(),
            new DefaultProblemDetailsFactory(Options.Create(options), null),
            Options.Create(options));
    }

    private class TestFormatter : IOutputFormatter
    {
        public bool CanWriteResult(OutputFormatterCanWriteContext context) => true;

        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            return context.HttpContext.Response.WriteAsJsonAsync(context.Object);
        }
    }

    private sealed class CapturingFormatter : IOutputFormatter
    {
        public OutputFormatterWriteContext LastContext { get; private set; }

        public bool CanWriteResult(OutputFormatterCanWriteContext context) => true;

        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            LastContext = context;
            return context.HttpContext.Response.WriteAsJsonAsync(context.Object);
        }
    }

    private sealed class RecordingWriter : IProblemDetailsWriter
    {
        public bool WriteAsyncCalled { get; private set; }

        public bool CanWrite(ProblemDetailsContext context) => true;

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            WriteAsyncCalled = true;
            return ValueTask.CompletedTask;
        }
    }

}
