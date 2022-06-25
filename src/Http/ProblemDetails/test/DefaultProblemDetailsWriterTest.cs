// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Tests;

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

public class DefaultProblemDetailsWriterTest
{
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(399)]
    public void CanWrite_IsFalse_ForSuccessStatus(int statusCode)
    {
        // Arrange
        var writer = GetWriter();
        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = statusCode },
        };

        // Act
        var canWrite = writer.CanWrite(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.False(canWrite);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(600)]
    [InlineData(700)]
    public void CanWrite_IsFalse_ForUnknownStatus(int statusCode)
    {
        // Arrange
        var writer = GetWriter();
        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = statusCode },
        };

        // Act
        var canWrite = writer.CanWrite(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.False(canWrite);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(499)]
    public void CanWrite_IsFalse_ForClientErrors(int statusCode)
    {
        // Arrange
        var writer = GetWriter();
        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = statusCode },
        };

        // Act
        var canWrite = writer.CanWrite(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.False(canWrite);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(599)]
    public void CanWrite_IsTrue_ForServerErrors(int statusCode)
    {
        // Arrange
        var writer = GetWriter();
        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = statusCode },
        };

        // Act
        var canWrite = writer.CanWrite(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.True(canWrite);
    }

    [Fact]
    public void CanWrite_IsTrue_ForRoutingErrors()
    {
        // Arrange
        var writer = GetWriter();
        var context = new DefaultHttpContext();

        // Act
        var canWrite = writer.CanWrite(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.True(canWrite);
    }

    [Fact]
    public async Task WriteAsync_Works()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream },
        };
        var expectedProblem = new ProblemDetails()
        {
            Detail = "Custom Bad Request",
            Instance = "Custom Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1-custom",
            Title = "Custom Bad Request",
        };

        //Act
        await writer.WriteAsync(
            context,
            statusCode: expectedProblem.Status,
            title: expectedProblem.Title,
            type: expectedProblem.Type,
            detail: expectedProblem.Detail,
            instance: expectedProblem.Instance);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream);
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
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream },
        };

        //Act
        await writer.WriteAsync(
            context,
            extensions: new Dictionary<string, object>
            {
                ["Extension1"] = "Extension1-Value",
                ["Extension2"] = "Extension2-Value",
            });

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream);
        Assert.NotNull(problemDetails);
        Assert.Collection(problemDetails.Extensions,
            (extension) =>
            {
                Assert.Equal("Extension1", extension.Key);
                Assert.Equal("Extension1-Value", extension.Value.ToString());
            },
            (extension) =>
            {
                Assert.Equal("Extension2", extension.Key);
                Assert.Equal("Extension2-Value", extension.Value.ToString());
            });
    }

    [Fact]
    public async Task WriteAsync_Applies_Defaults()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status500InternalServerError },
        };

        //Act
        await writer.WriteAsync(context);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream);
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.1", problemDetails.Type);
        Assert.Equal("An error occurred while processing your request.", problemDetails.Title);
    }

    [Fact]
    public async Task WriteAsync_Applies_CustomConfiguration()
    {
        // Arrange
        var options = new ProblemDetailsOptions()
        {
            ConfigureDetails = (context, problem) =>
            {
                problem.Status = StatusCodes.Status406NotAcceptable;
                problem.Title = "Custom Title";
                problem.Extensions["new-extension"] = new { TraceId = Guid.NewGuid() };
            }
        };
        var writer = GetWriter(options);
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status500InternalServerError },
        };

        //Act
        await writer.WriteAsync(context, statusCode: StatusCodes.Status400BadRequest);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream);
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status406NotAcceptable, problemDetails.Status);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", problemDetails.Type);
        Assert.Equal("Custom Title", problemDetails.Title);
        Assert.Contains("new-extension", problemDetails.Extensions);
    }

    [Fact]
    public async Task WriteAsync_UsesStatusCode_FromProblemDetails_WhenSpecified()
    {
        // Arrange
        var writer = GetWriter();
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status500InternalServerError },
        };

        //Act
        await writer.WriteAsync(context, statusCode: StatusCodes.Status400BadRequest);

        //Assert
        stream.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream);
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", problemDetails.Type);
        Assert.Equal("Bad Request", problemDetails.Title);
    }

    private DefaultProblemDetailsWriter GetWriter(ProblemDetailsOptions options = null)
    {
        options ??= new ProblemDetailsOptions();
        return new DefaultProblemDetailsWriter(Options.Create(options));
    }
}
