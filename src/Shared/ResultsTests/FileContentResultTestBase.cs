// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Internal;

public abstract class FileContentResultTestBase
{
    protected abstract Task ExecuteAsync(
        HttpContext httpContext,
        byte[] buffer,
        string contentType,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null,
        bool enableRangeProcessing = false);

    [Fact]
    public async Task WriteFileAsync_CopiesBuffer_ToOutputStream()
    {
        // Arrange
        var buffer = new byte[] { 1, 2, 3, 4, 5 };
        var httpContext = GetHttpContext();

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        // Act
        await ExecuteAsync(httpContext, buffer, "text/plain");

        // Assert
        Assert.Equal(buffer, outStream.ToArray());
    }

    [Theory]
    [InlineData(0L, 4L, "Hello", 5L)]
    [InlineData(6L, 10L, "World", 5L)]
    [InlineData(null, 5L, "World", 5L)]
    [InlineData(6L, null, "World", 5L)]
    public async Task WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(
        long? start,
        long? end,
        string expectedString,
        long contentLength)
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");

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
        await ExecuteAsync(httpContext, byteArray, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        start = start ?? 11 - end;
        end = start + contentLength - 1;
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        var contentRange = new ContentRangeHeaderValue(start.Value, end.Value, byteArray.Length);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.Equal(contentLength, httpResponse.ContentLength);
        Assert.Equal(expectedString, body);
    }

    [Fact]
    public async Task WriteFileAsync_IfRangeHeaderValid_WritesRangeRequest()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = DateTimeOffset.MinValue;
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");

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
        await ExecuteAsync(httpContext, byteArray, contentType, lastModified, entityTag: entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);

        Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        var contentRange = new ContentRangeHeaderValue(0, 4, byteArray.Length);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.Equal(5, httpResponse.ContentLength);
        Assert.Equal("Hello", body);
    }

    [Fact]
    public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestIgnored()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = DateTimeOffset.MinValue.AddDays(1);
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");

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
        await ExecuteAsync(httpContext, byteArray, contentType, lastModified, entityTag);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal("Hello World", body);
    }

    [Fact]
    public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestIgnored()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = DateTimeOffset.MinValue.AddDays(1);
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");

        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfMatch = new[]
        {
                new EntityTagHeaderValue("\"Etag\""),
            };
        requestHeaders.Range = new RangeHeaderValue(0, 4);
        requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, byteArray, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal("Hello World", body);
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

        var httpContext = GetHttpContext();
        httpContext.Request.Headers.Range = rangeString;
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, byteArray, contentType, lastModified, entityTag, enableRangeProcessing: true);

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

        var httpContext = GetHttpContext();
        httpContext.Request.Headers.Range = rangeString;
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, byteArray, contentType, lastModified, entityTag, enableRangeProcessing: true);

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
    }

    [Fact]
    public async Task WriteFileAsync_PreconditionFailed_RangeRequestedIgnored()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");

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
        await ExecuteAsync(httpContext, byteArray, contentType, lastModified, entityTag, enableRangeProcessing: true);

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
    }

    [Fact]
    public async Task WriteFileAsync_NotModified_RangeRequestedIgnored()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");

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
        await ExecuteAsync(httpContext, byteArray, contentType, lastModified, entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(StatusCodes.Status304NotModified, httpResponse.StatusCode);
        Assert.Null(httpResponse.ContentLength);
        Assert.False(httpResponse.Headers.ContainsKey(HeaderNames.ContentType));
        Assert.Empty(httpResponse.Headers.ContentRange);
        Assert.NotEmpty(httpResponse.Headers.LastModified);
        Assert.Empty(body);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
    {
        // Arrange
        var expectedContentType = "text/foo; charset=us-ascii";
        var buffer = new byte[] { 1, 2, 3, 4, 5 };

        var httpContext = GetHttpContext();

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        // Act
        await ExecuteAsync(httpContext, buffer, expectedContentType);

        // Assert
        Assert.Equal(buffer, outStream.ToArray());
        Assert.Equal(expectedContentType, httpContext.Response.ContentType);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
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
