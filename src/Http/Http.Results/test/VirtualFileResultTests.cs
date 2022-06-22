// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class VirtualFileResultTests : VirtualFileResultTestBase
{
    protected override Task ExecuteAsync(HttpContext httpContext, string path, string contentType, DateTimeOffset? lastModified = null, EntityTagHeaderValue entityTag = null, bool enableRangeProcessing = false)
    {
        var result = new VirtualFileHttpResult(path, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            EnableRangeProcessing = enableRangeProcessing,
        };

        return result.ExecuteAsync(httpContext);
    }

    [Fact]
    public void VirtualFileHttpResult_Implements_IFileHttpResult_Correctly()
    {
        // Arrange & Act
        var contentType = "application/x-zip";
        var downloadName = "sample.zip";
        var result = new VirtualFileHttpResult("~/file.zip", contentType)
        {
            FileDownloadName = downloadName
        } as IFileHttpResult;

        // Assert
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(downloadName, result.FileDownloadName);
    }

    [Fact]
    public void VirtualFileHttpResult_Implements_IContentTypeHttpResult_Correctly()
    {
        // Arrange & Act
        var contentType = "application/x-zip";
        var downloadName = "sample.zip";
        var result = new VirtualFileHttpResult("~/file.zip", contentType)
        {
            FileDownloadName = downloadName
        } as IContentTypeHttpResult;

        // Assert
        Assert.Equal(contentType, result.ContentType);
    }
}
