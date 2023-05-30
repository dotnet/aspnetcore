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
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
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
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
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
    public void CanWrite_ReturnsFalse_WhenNotController()
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
    public void CanWrite_ReturnsTrue_WhenController()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = CreateContext(stream, metadata: new EndpointMetadataCollection(new ControllerAttribute()));

        //Act
        var result = writer.CanWrite(new() { HttpContext = context });

        //Assert
        Assert.True(result);
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

}
