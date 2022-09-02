// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Internal;

public abstract class FileStreamResultTestBase
{
    protected abstract Task ExecuteAsync(
        HttpContext httpContext,
        Stream stream,
        string contentType,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null,
        bool enableRangeProcessing = false);

    [Theory]
    [InlineData(0L, 4L, "Hello", 5L)]
    [InlineData(6L, 10L, "World", 5L)]
    [InlineData(null, 5L, "World", 5L)]
    [InlineData(6L, null, "World", 5L)]
    public async Task WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");
        var readStream = new MemoryStream(byteArray);
        readStream.SetLength(11);

        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.Range = new RangeHeaderValue(start, end);
        requestHeaders.IfMatch = new[]
        {
                new EntityTagHeaderValue("\"Etag\""),
            };
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, readStream, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        start = start ?? 11 - end;
        end = start + contentLength - 1;
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        var contentRange = new ContentRangeHeaderValue(start.Value, end.Value, byteArray.Length);
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.Equal(contentLength, httpResponse.ContentLength);
        Assert.Equal(expectedString, body);
        Assert.False(readStream.CanSeek);
    }

    [Fact]
    public async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = DateTimeOffset.MinValue;
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");
        var readStream = new MemoryStream(byteArray);
        readStream.SetLength(11);

        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfMatch = new[]
        {
                new EntityTagHeaderValue("\"Etag\""),
            };
        requestHeaders.Range = new RangeHeaderValue(0, 4);
        requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, readStream, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        var contentRange = new ContentRangeHeaderValue(0, 4, byteArray.Length);
        Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.Equal(5, httpResponse.ContentLength);
        Assert.Equal("Hello", body);
        Assert.False(readStream.CanSeek);
    }

    [Fact]
    public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = DateTimeOffset.MinValue;
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");
        var readStream = new MemoryStream(byteArray);
        readStream.SetLength(11);

        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfMatch = new[]
        {
                new EntityTagHeaderValue("\"Etag\""),
            };
        requestHeaders.Range = new RangeHeaderValue(0, 4);
        requestHeaders.IfRange = new RangeConditionHeaderValue(DateTimeOffset.MinValue);
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, readStream, contentType, lastModified, entityTag);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal("Hello World", body);
        Assert.False(readStream.CanSeek);
    }

    [Fact]
    public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = DateTimeOffset.MinValue.AddDays(1);
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");
        var readStream = new MemoryStream(byteArray);
        readStream.SetLength(11);

        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfMatch = new[]
        {
                new EntityTagHeaderValue("\"Etag\""),
            };
        requestHeaders.Range = new RangeHeaderValue(0, 4);
        requestHeaders.IfRange = new RangeConditionHeaderValue(DateTimeOffset.MinValue);
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, readStream, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal("Hello World", body);
        Assert.False(readStream.CanSeek);
    }

    [Theory]
    [InlineData("0-5")]
    [InlineData("bytes = ")]
    [InlineData("bytes = 1-4, 5-11")]
    public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(string rangeString)
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");
        var readStream = new MemoryStream(byteArray);

        var httpContext = GetHttpContext();
        httpContext.Request.Headers.Range = rangeString;
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, readStream, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Empty(httpResponse.Headers.ContentRange);
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal("Hello World", body);
        Assert.False(readStream.CanSeek);
    }

    [Theory]
    [InlineData("bytes = 12-13")]
    [InlineData("bytes = -0")]
    public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(string rangeString)
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");
        var readStream = new MemoryStream(byteArray);

        var httpContext = GetHttpContext();
        httpContext.Request.Headers.Range = rangeString;
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, readStream, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        var contentRange = new ContentRangeHeaderValue(byteArray.Length);
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.Equal(0, httpResponse.ContentLength);
        Assert.Empty(body);
        Assert.False(readStream.CanSeek);
    }

    [Fact]
    public async Task WriteFileAsync_RangeRequested_PreconditionFailed()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");
        var readStream = new MemoryStream(byteArray);

        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfMatch = new[]
        {
                new EntityTagHeaderValue("\"NotEtag\""),
            };
        httpContext.Request.Headers.Range = "bytes = 0-6";
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, readStream, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(StatusCodes.Status412PreconditionFailed, httpResponse.StatusCode);
        Assert.Null(httpResponse.ContentLength);
        Assert.Empty(httpResponse.Headers.ContentRange);
        Assert.NotEmpty(httpResponse.Headers.LastModified);
        Assert.Empty(body);
        Assert.False(readStream.CanSeek);
    }

    [Fact]
    public async Task WriteFileAsync_NotModified_RangeRequestedIgnored()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");
        var readStream = new MemoryStream(byteArray);

        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfNoneMatch = new[]
        {
                new EntityTagHeaderValue("\"Etag\""),
            };
        httpContext.Request.Headers.Range = "bytes = 0-6";
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, readStream, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(StatusCodes.Status304NotModified, httpResponse.StatusCode);
        Assert.Null(httpResponse.ContentLength);
        Assert.Empty(httpResponse.Headers.ContentRange);
        Assert.False(httpResponse.Headers.ContainsKey(HeaderNames.ContentType));
        Assert.NotEmpty(httpResponse.Headers.LastModified);
        Assert.Empty(body);
        Assert.False(readStream.CanSeek);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(null)]
    public async Task WriteFileAsync_RangeRequested_FileLengthZeroOrNull(long? fileLength)
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("");
        var readStream = new MemoryStream(byteArray);
        fileLength = fileLength ?? 0L;
        readStream.SetLength(fileLength.Value);

        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.Range = new RangeHeaderValue(0, 5);
        requestHeaders.IfMatch = new[]
        {
                new EntityTagHeaderValue("\"Etag\""),
            };
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, readStream, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        var contentRange = new ContentRangeHeaderValue(byteArray.Length);
        Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.Equal(0, httpResponse.ContentLength);
        Assert.Empty(body);
        Assert.False(readStream.CanSeek);
    }

    [Fact]
    public async Task WriteFileAsync_CopiesProvidedStream_ToOutputStream()
    {
        // Arrange
        // Generate an array of bytes with a predictable pattern
        // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, A, B, C, D, E, F, 10, 11, 12, 13
        var originalBytes = Enumerable.Range(0, 0x1234)
            .Select(b => (byte)(b % 20)).ToArray();

        var originalStream = new MemoryStream(originalBytes);

        var httpContext = GetHttpContext();
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        // Act
        await ExecuteAsync(httpContext, originalStream, "text/plain");

        // Assert
        var outBytes = outStream.ToArray();
        Assert.True(originalBytes.SequenceEqual(outBytes));
        Assert.False(originalStream.CanSeek);
    }

    [Fact]
    public async Task SetsSuppliedContentTypeAndEncoding()
    {
        // Arrange
        var expectedContentType = "text/foo; charset=us-ascii";
        // Generate an array of bytes with a predictable pattern
        // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, A, B, C, D, E, F, 10, 11, 12, 13
        var originalBytes = Enumerable.Range(0, 0x1234)
            .Select(b => (byte)(b % 20)).ToArray();

        var originalStream = new MemoryStream(originalBytes);

        var httpContext = GetHttpContext();
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        // Act
        await ExecuteAsync(httpContext, originalStream, expectedContentType);

        // Assert
        var outBytes = outStream.ToArray();
        Assert.True(originalBytes.SequenceEqual(outBytes));
        Assert.Equal(expectedContentType, httpContext.Response.ContentType);
        Assert.False(originalStream.CanSeek);
    }

    [Fact]
    public async Task HeadRequest_DoesNotWriteToBody_AndClosesReadStream()
    {
        // Arrange
        var readStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));

        var httpContext = GetHttpContext();
        httpContext.Request.Method = "HEAD";
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        // Act
        await ExecuteAsync(httpContext, readStream, "text/plain");

        // Assert
        Assert.False(readStream.CanSeek);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();
        return httpContext;
    }
}
