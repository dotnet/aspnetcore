// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Tests;

public class ProblemDetailsServiceTest
{
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
    public async Task WriteAsync_Skip_NextWriters_WhenResponseAlreadyStarted()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter>
            {
                new MetadataBasedWriter("FirstWriter", canWrite: false),
                new MetadataBasedWriter("SecondWriter"),
                new MetadataBasedWriter("FirstWriter"),
            },
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
        await service.WriteAsync(new ProblemDetailsContext(context) { AdditionalMetadata = metadata });

        // Assert
        Assert.Equal("\"SecondWriter\"", Encoding.UTF8.GetString(stream.ToArray()));
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
        await service.WriteAsync(new ProblemDetailsContext(context));

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
        await service.WriteAsync(new ProblemDetailsContext(context));

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
        await service.WriteAsync(new ProblemDetailsContext(context));

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
        await service.WriteAsync(new ProblemDetailsContext(context));

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
        private readonly string _content;
        private readonly bool _canWrite;

        public MetadataBasedWriter(string content = "Content", bool canWrite = true)
        {
            _content = content;
            _canWrite = canWrite;
        }

        public async ValueTask<bool> WriteAsync(ProblemDetailsContext context)
        {
            var metadata = context.AdditionalMetadata?.GetMetadata<SampleMetadata>() ??
                context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<SampleMetadata>();

            if (metadata != null && _canWrite)
            {
                await context.HttpContext.Response.WriteAsJsonAsync(_content);
                return true;
            }

            return false;
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
