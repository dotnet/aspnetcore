// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class ProblemDetailsServiceTest
{
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
            });

        var metadata = new EndpointMetadataCollection(new SampleMetadata() { ContentType = "application/problem+json" });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act
        await service.WriteAsync(new() { HttpContext = context, AdditionalMetadata = metadata });

        // Assert
        Assert.Equal("\"SecondWriter\"", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenNoWriterRegistered()
    {
        // Arrange
        var service = CreateService();
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act
        await service.WriteAsync(new() { HttpContext = context });

        // Assert
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenNoWriterCanWrite()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act
        await service.WriteAsync(new() { HttpContext = context });

        // Assert
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Theory]
    [InlineData(StatusCodes.Status100Continue)]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status300MultipleChoices)]
    [InlineData(399)]
    public async Task WriteAsync_Skip_WhenSuccessStatusCode(int statusCode)
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = statusCode },
        };
        var metadata = new EndpointMetadataCollection(new SampleMetadata() { ContentType = "application/problem+json" });
        context.SetEndpoint(new Endpoint(context => Task.CompletedTask, metadata, null));

        // Act
        await service.WriteAsync(new() { HttpContext = context });

        // Assert
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static ProblemDetailsService CreateService(
        IEnumerable<IProblemDetailsWriter> writers = null)
    {
        writers ??= Array.Empty<IProblemDetailsWriter>();
        return new ProblemDetailsService(writers);
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

        public bool CanWrite(ProblemDetailsContext context)
        {
            var metadata = context.AdditionalMetadata?.GetMetadata<SampleMetadata>() ??
                context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<SampleMetadata>();

            return metadata != null && _canWrite;

        }

        public ValueTask WriteAsync(ProblemDetailsContext context)
            => new(context.HttpContext.Response.WriteAsJsonAsync(_content));
    }
}
