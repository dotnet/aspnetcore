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
    public async Task TryWriteAsync_ReturnsTrue_WhenAtLeastOneWriterCanWrite()
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
        var result = await service.TryWriteAsync(new() { HttpContext = context, AdditionalMetadata = metadata });

        // Assert
        Assert.True(result);
        Assert.Equal("\"SecondWriter\"", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_Throws_WhenNoWriterRegistered()
    {
        // Arrange
        var service = CreateService();
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.WriteAsync(new() { HttpContext = context }));
    }

    [Fact]
    public async Task WriteAsync_Throws_WhenNoWriterCanWrite()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.WriteAsync(new() { HttpContext = context }));
    }

    [Fact]
    public async Task TryWriteAsync_ReturnsFalse_WhenNoWriterRegistered()
    {
        // Arrange
        var service = CreateService();
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act
        var result = await service.TryWriteAsync(new() { HttpContext = context });

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task TryWriteAsync_ReturnsFalse_WhenNoWriterCanWrite()
    {
        // Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        // Act & Assert
        var result = await service.TryWriteAsync(new() { HttpContext = context });

        // Assert
        Assert.False(result);
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
