// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc;

public class PushFileStreamResultTest : PushFileStreamResultTestBase
{
    protected override Task ExecuteAsync(
        HttpContext httpContext,
        Func<Stream, Task> streamWriterCallback,
        string contentType,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null)
    {
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<ILoggerFactory, NullLoggerFactory>()
            .AddSingleton<IActionResultExecutor<PushFileStreamResult>, PushFileStreamResultExecutor>()
            .BuildServiceProvider();

        var actionContext = new ActionContext(httpContext, new(), new());
        var fileStreamResult = new PushFileStreamResult(streamWriterCallback, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
        };

        return fileStreamResult.ExecuteResultAsync(actionContext);
    }

    [Fact]
    public void Constructor_SetsContentType()
    {
        // Arrange
        var streamWriterCallback = (Stream _) => Task.CompletedTask;
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var expectedMediaType = contentType;

        // Act
        var result = new PushFileStreamResult(streamWriterCallback, contentType);

        // Assert
        Assert.Equal(expectedMediaType, result.ContentType);
    }

    [Fact]
    public void Constructor_SetsLastModifiedAndEtag()
    {
        // Arrange
        var streamWriterCallback = (Stream _) => Task.CompletedTask;
        var contentType = "text/plain";
        var expectedMediaType = contentType;
        var lastModified = new DateTimeOffset();
        var entityTag = new EntityTagHeaderValue("\"Etag\"");

        // Act
        var result = new PushFileStreamResult(streamWriterCallback, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
        };

        // Assert
        Assert.Equal(lastModified, result.LastModified);
        Assert.Equal(entityTag, result.EntityTag);
        Assert.Equal(expectedMediaType, result.ContentType);
    }
}
