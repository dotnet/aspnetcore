// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class FileContentResultTests : FileContentResultTestBase
{
    protected override Task ExecuteAsync(
        HttpContext httpContext,
        byte[] buffer,
        string contentType,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null,
        bool enableRangeProcessing = false)
    {
        var result = new FileContentHttpResult(buffer, contentType)
        {
            EntityTag = entityTag,
            LastModified = lastModified,
            EnableRangeProcessing = enableRangeProcessing,
        };

        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton<ILoggerFactory, NullLoggerFactory>()
            .BuildServiceProvider();

        return result.ExecuteAsync(httpContext);
    }

    [Fact]
    public void FileContentResult_Implements_IFileHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/x-zip";
        var downloadName = "sample.zip";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IFileHttpResult>(new FileContentHttpResult(Array.Empty<byte>(), contentType) { FileDownloadName = downloadName });
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(downloadName, result.FileDownloadName);
    }

    [Fact]
    public void FileContentResult_Implements_IContentTypeHttpResult_Correctly()
    {
        // Arrange & Act
        var contentType = "application/x-zip";
        var downloadName = "sample.zip";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IContentTypeHttpResult>(new FileContentHttpResult(Array.Empty<byte>(), contentType) { FileDownloadName = downloadName });
        Assert.Equal(contentType, result.ContentType);
    }
}
