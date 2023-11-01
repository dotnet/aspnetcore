// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Internal;

public abstract class PushFileStreamResultTestBase
{
    protected abstract Task ExecuteAsync(
        HttpContext httpContext,
        Func<Stream, Task> streamWriterCallback,
        string contentType,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null);

    [Fact]
    public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored()
    {
        // Arrange
        var contentType = "text/plain";
        var lastModified = DateTimeOffset.MinValue;
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var byteArray = Encoding.ASCII.GetBytes("Hello World");
        var streamWriterCallback = (Stream stream) => stream.WriteAsync(byteArray).AsTask();

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
        await ExecuteAsync(httpContext, streamWriterCallback, contentType, lastModified, entityTag);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal("Hello World", body);
    }

    [Fact]
    public async Task WriteFileAsync_CopiesProvidedData_ToOutputStream()
    {
        // Arrange
        // Generate an array of bytes with a predictable pattern
        // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, A, B, C, D, E, F, 10, 11, 12, 13
        var originalBytes = Enumerable.Range(0, 0x1234)
            .Select(b => (byte)(b % 20)).ToArray();

        var streamWriterCallback = (Stream stream) => stream.WriteAsync(originalBytes).AsTask();

        var httpContext = GetHttpContext();
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        // Act
        await ExecuteAsync(httpContext, streamWriterCallback, "text/plain");

        // Assert
        var outBytes = outStream.ToArray();
        Assert.True(originalBytes.SequenceEqual(outBytes));
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

        var streamWriterCallback = (Stream stream) => stream.WriteAsync(originalBytes).AsTask();

        var httpContext = GetHttpContext();
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        // Act
        await ExecuteAsync(httpContext, streamWriterCallback, expectedContentType);

        // Assert
        var outBytes = outStream.ToArray();
        Assert.True(originalBytes.SequenceEqual(outBytes));
        Assert.Equal(expectedContentType, httpContext.Response.ContentType);
    }

    [Fact]
    public async Task HeadRequest_DoesNotWriteToBody()
    {
        // Arrange
        var streamWriterCallback = (Stream stream) => stream.WriteAsync("Hello, World!"u8.ToArray()).AsTask();

        var httpContext = GetHttpContext();
        httpContext.Request.Method = "HEAD";
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        // Act
        await ExecuteAsync(httpContext, streamWriterCallback, "text/plain");

        // Assert
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
