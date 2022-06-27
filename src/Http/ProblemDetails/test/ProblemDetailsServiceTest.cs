// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Http.Tests;

public class ProblemDetailsServiceTest
{
    [Fact]
    public void GetWriter_ReturnsNull_WhenNotEnabled()
    {
        // Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.None });
        var context = new DefaultHttpContext();

        // Act
        var writer = service.GetWriter(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.Null(writer);
    }

    [Fact]
    public void GetWriter_ReturnsNull_WhenNotRegisteredWriters()
    {
        // Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.All });
        var context = new DefaultHttpContext();

        // Act
        var writer = service.GetWriter(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.Null(writer);
    }

    [Fact]
    public void GetWriter_ReturnsNull_WhenNoWriterCanWrite()
    {
        // Arrange
        var writers = new List<IProblemDetailsWriter>() {
            Mock.Of<IProblemDetailsWriter>(w => w.CanWrite(It.IsAny<HttpContext>(), It.IsAny<EndpointMetadataCollection>()) == false),
            Mock.Of<IProblemDetailsWriter>(w => w.CanWrite(It.IsAny<HttpContext>(), It.IsAny<EndpointMetadataCollection>()) == false)
        };
        var service = CreateService(
            writers: writers,
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.All });
        var context = new DefaultHttpContext();

        // Act
        var writer = service.GetWriter(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.Null(writer);
    }

    [Fact]
    public void GetWriter_Returns_ForContextMetadata()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.All });

        var context = new DefaultHttpContext();
        var metadata = new EndpointMetadataCollection(new SampleMetadata() { ContentType = "application/problem+json"});
        context.SetEndpoint(new Endpoint(context => Task.CompletedTask, metadata, null));

        // Act
        var selectedWriter = service.GetWriter(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.NotNull(selectedWriter);
        Assert.IsType<MetadataBasedWriter>(selectedWriter);
    }

    [Fact]
    public void GetWriter_Returns_ForSpecifiedMetadata()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.All });

        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = StatusCodes.Status400BadRequest }
        };
        var metadata = new EndpointMetadataCollection(new SampleMetadata() { ContentType = "application/problem+json" });
        context.SetEndpoint(new Endpoint(context => Task.CompletedTask, EndpointMetadataCollection.Empty, null));

        // Act
        var selectedWriter = service.GetWriter(context, additionalMetadata: metadata);

        // Assert
        Assert.NotNull(selectedWriter);
        Assert.IsType<MetadataBasedWriter>(selectedWriter);
    }

    [Fact]
    public void GetWriter_Returns_FirstCanWriter()
    {
        // Arrange
        var writer1 = Mock.Of<IProblemDetailsWriter>(w => w.CanWrite(It.IsAny<HttpContext>(), It.IsAny<EndpointMetadataCollection>()) == true);
        var writer2 = Mock.Of<IProblemDetailsWriter>(w => w.CanWrite(It.IsAny<HttpContext>(), It.IsAny<EndpointMetadataCollection>()) == true);
        var writers = new List<IProblemDetailsWriter>() { writer1, writer2 };
        var service = CreateService(
            writers: writers,
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.All });
        var context = new DefaultHttpContext();

        // Act
        var selectedWriter = service.GetWriter(context, EndpointMetadataCollection.Empty);

        // Assert
        Assert.NotNull(selectedWriter);
        Assert.Equal(writer1, selectedWriter);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(500)]
    public void CalculateProblemType_IsNone_WhenNoMetadata(int statusCode)
    {
        // Arrange & Act
        var problemType = ProblemDetailsService.CalculateProblemType(
            statusCode,
            metadataCollection: EndpointMetadataCollection.Empty,
            additionalMetadata: EndpointMetadataCollection.Empty);

        // Assert
        Assert.Equal(ProblemDetailsTypes.None, problemType);
    }

    [Theory]
    [InlineData(100, ProblemDetailsTypes.All)]
    [InlineData(200, ProblemDetailsTypes.All)]
    [InlineData(400, ProblemDetailsTypes.Server)]
    [InlineData(400, ProblemDetailsTypes.None)]
    [InlineData(500, ProblemDetailsTypes.Client)]
    [InlineData(500, ProblemDetailsTypes.None)]
    public void CalculateProblemType_IsNone_WhenNotAllowed(int statusCode, ProblemDetailsTypes metadataProblemType)
    {
        // Arrange & Act
        var problemType = ProblemDetailsService.CalculateProblemType(
            statusCode,
            metadataCollection: new EndpointMetadataCollection(new TestProblemMetadata(metadataProblemType)),
            additionalMetadata: EndpointMetadataCollection.Empty);

        // Assert
        Assert.Equal(ProblemDetailsTypes.None, problemType);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    public void CalculateProblemType_CanBeRouting_ForAllStatusCode(int statusCode)
    {
        // Arrange & Act
        var problemType = ProblemDetailsService.CalculateProblemType(
            statusCode,
            metadataCollection: new EndpointMetadataCollection(new TestProblemMetadata(ProblemDetailsTypes.Routing)),
            additionalMetadata: EndpointMetadataCollection.Empty);

        // Assert
        Assert.Equal(ProblemDetailsTypes.Routing, problemType);
    }

    [Theory]
    [InlineData(400, ProblemDetailsTypes.Client)]
    [InlineData(400, ProblemDetailsTypes.Routing)]
    [InlineData(500, ProblemDetailsTypes.Server)]
    [InlineData(500, ProblemDetailsTypes.Routing)]
    public void CalculateProblemType_IsCorrect_WhenMetadata_WithStatusCode(int statusCode, ProblemDetailsTypes metadataProblemType)
    {
        // Arrange & Act
        var problemType = ProblemDetailsService.CalculateProblemType(
            statusCode,
            metadataCollection: new EndpointMetadataCollection(new TestProblemMetadata(statusCode, metadataProblemType)),
            additionalMetadata: EndpointMetadataCollection.Empty);

        // Assert
        Assert.Equal(metadataProblemType, problemType);
    }

    [Fact]
    public void CalculateProblemType_PrefersAdditionalMetadata()
    {
        // Arrange & Act
        var statusCode = StatusCodes.Status400BadRequest;
        var problemType = ProblemDetailsService.CalculateProblemType(
            statusCode,
            metadataCollection: new EndpointMetadataCollection(new TestProblemMetadata(statusCode, ProblemDetailsTypes.Client)),
            additionalMetadata: new EndpointMetadataCollection(new TestProblemMetadata(statusCode, ProblemDetailsTypes.Routing)));

        // Assert
        Assert.Equal(ProblemDetailsTypes.Routing, problemType);
    }

    [Fact]
    public void CalculateProblemType_PrefersMetadataWithStatusCode()
    {
        // Arrange & Act
        var statusCode = StatusCodes.Status400BadRequest;
        var problemType = ProblemDetailsService.CalculateProblemType(
            statusCode,
            metadataCollection: new EndpointMetadataCollection(new TestProblemMetadata(statusCode, ProblemDetailsTypes.Client)),
            additionalMetadata: new EndpointMetadataCollection(new TestProblemMetadata(ProblemDetailsTypes.Routing)));

        // Assert
        Assert.Equal(ProblemDetailsTypes.Client, problemType);
    }

    [Fact]
    public async Task WriteAsync_Call_SelectedWriter()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.Client | ProblemDetailsTypes.Server });

        var metadata = new EndpointMetadataCollection(
            new SampleMetadata() { ContentType = "application/problem+json" },
            new TestProblemMetadata());
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act
        await service.WriteAsync(context, additionalMetadata: metadata);

        // Assert
        Assert.Equal("\"Content\"", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenNoWriterRegistered()
    {
        // Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.All });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act
        await service.WriteAsync(context);

        // Assert
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenNoWriterSelected()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.All });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act
        await service.WriteAsync(context);

        // Assert
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenNotEnabled()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.None });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };
        var metadata = new EndpointMetadataCollection(
            new SampleMetadata() { ContentType = "application/problem+json" },
            new TestProblemMetadata(context.Response.StatusCode, ProblemDetailsTypes.All));
        context.SetEndpoint(new Endpoint(context => Task.CompletedTask, metadata, null));

        // Act
        await service.WriteAsync(context);

        // Assert
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenNotAllowed()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedProblemTypes = ProblemDetailsTypes.All });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };
        var metadata = new EndpointMetadataCollection(
            new SampleMetadata() { ContentType = "application/problem+json" },
            new TestProblemMetadata(context.Response.StatusCode, ProblemDetailsTypes.None));
        context.SetEndpoint(new Endpoint(context => Task.CompletedTask, metadata, null));

        // Act
        await service.WriteAsync(context);

        // Assert
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static ProblemDetailsService CreateService(
        ProblemDetailsOptions options,
        IEnumerable<IProblemDetailsWriter> writers = null)
    {
        writers ??= Array.Empty<IProblemDetailsWriter>();
        return new ProblemDetailsService(writers, Options.Create(options));
    }

    private class SampleMetadata
    {
        public string ContentType { get; set; }
    }

    private class MetadataBasedWriter : IProblemDetailsWriter
    {
        public bool CanWrite(HttpContext context, EndpointMetadataCollection additionalMetadata)
        {
            var metadata = additionalMetadata?.GetMetadata<SampleMetadata>() ??
                context.GetEndpoint()?.Metadata.GetMetadata<SampleMetadata>();
            return metadata != null;
        }

        public Task WriteAsync(HttpContext context, int? statusCode = null, string title = null, string type = null, string detail = null, string instance = null, IDictionary<string, object> extensions = null)
        {
            return context.Response.WriteAsJsonAsync("Content");
        }
    }

    private class TestProblemMetadata : IProblemDetailsMetadata
    {
        public TestProblemMetadata()
        {
            ProblemType = ProblemDetailsTypes.All;
        }

        public TestProblemMetadata(ProblemDetailsTypes problemTypes)
        {
            ProblemType = problemTypes;
        }

        public TestProblemMetadata(int status, ProblemDetailsTypes problemTypes)
        {
            StatusCode = status;
            ProblemType = problemTypes;
        }

        public int? StatusCode { get;}

        public ProblemDetailsTypes ProblemType { get; }
    }
}
