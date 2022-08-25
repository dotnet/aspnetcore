// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class PushStreamResultTests
{
    [Fact]
    public async Task PushStreamResultsExposeTheResponseBody()
    {
        var result = new PushStreamHttpResult(body => body.WriteAsync(Encoding.UTF8.GetBytes("Hello World").AsMemory()).AsTask(), contentType: null);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider()
        };
        var ms = new MemoryStream();
        httpContext.Response.Body = ms;

        await result.ExecuteAsync(httpContext);

        Assert.Equal("Hello World", Encoding.UTF8.GetString(ms.ToArray()));
        Assert.Equal("application/octet-stream", result.ContentType);
    }

    [Fact]
    public void Constructor_SetsContentTypeAndParameters()
    {
        // Arrange
        var stream = Stream.Null;
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var expectedMediaType = contentType;
        var callback = (Stream body) => body.WriteAsync(Encoding.UTF8.GetBytes("Hello World").AsMemory()).AsTask();

        // Act
        var result = new PushStreamHttpResult(callback, contentType);

        // Assert
        Assert.Equal(expectedMediaType, result.ContentType);
    }

    [Fact]
    public void Constructor_SetsLastModifiedAndEtag()
    {
        // Arrange
        var stream = Stream.Null;
        var contentType = "text/plain";
        var expectedMediaType = contentType;
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var callback = (Stream body) => body.WriteAsync(Encoding.UTF8.GetBytes("Hello World").AsMemory()).AsTask();

        // Act
        var result = new PushStreamHttpResult(callback, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
        };

        // Assert
        Assert.Equal(lastModified, result.LastModified);
        Assert.Equal(entityTag, result.EntityTag);
        Assert.Equal(expectedMediaType, result.ContentType);
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new PushStreamHttpResult(s => Task.CompletedTask, "content-type");
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PushStreamResult_Implements_IFileHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/x-zip";
        var downloadName = "sample.zip";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IFileHttpResult>(new PushStreamHttpResult(s => Task.CompletedTask, contentType) { FileDownloadName = downloadName });
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(downloadName, result.FileDownloadName);
    }

    [Fact]
    public void PushStreamResult_Implements_IContentTypeHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/x-zip";
        var downloadName = "sample.zip";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IContentTypeHttpResult>(new PushStreamHttpResult(s => Task.CompletedTask, contentType) { FileDownloadName = downloadName });
        Assert.Equal(contentType, result.ContentType);
    }
}
