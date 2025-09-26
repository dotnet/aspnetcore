// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.StaticAssets.Internal;

namespace Microsoft.AspNetCore.StaticAssets.Tests;

public class NonAsciiHeaderEncodingTests
{
    [Fact]
    public void HeaderValueEncoder_EncodesNonAsciiInLinkHeader_SingleValue()
    {
        // Arrange
        var linkHeader = "</_content/项目/app.js>; rel=modulepreload";

        // Act
        var result = HeaderValueEncoder.Sanitize("Link", linkHeader);

        // Assert
        Assert.Equal("</_content/%E9%A1%B9%E7%9B%AE/app.js>; rel=modulepreload", result);
    }

    [Fact]
    public void HeaderValueEncoder_EncodesNonAsciiInLinkHeader_MultipleValues()
    {
        // Arrange
        var linkHeader = "</_content/项目/app.js>; rel=modulepreload, </样式/site.css>; as=style; rel=preload";

        // Act
        var result = HeaderValueEncoder.Sanitize("Link", linkHeader);

        // Assert
        var expected = "</_content/%E9%A1%B9%E7%9B%AE/app.js>; rel=modulepreload, </%E6%A0%B7%E5%BC%8F/site.css>; as=style; rel=preload";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void HeaderValueEncoder_LeavesAbsoluteUrlsUnchanged()
    {
        // Arrange
        var linkHeader = "<https://example.com/path>; rel=preload";

        // Act
        var result = HeaderValueEncoder.Sanitize("Link", linkHeader);

        // Assert
        Assert.Equal(linkHeader, result);
    }

    [Fact]
    public void HeaderValueEncoder_LeavesAsciiHeadersUnchanged()
    {
        // Arrange
        var linkHeader = "</_content/app/app.js>; rel=modulepreload";

        // Act
        var result = HeaderValueEncoder.Sanitize("Link", linkHeader);

        // Assert
        Assert.Equal(linkHeader, result);
    }

    [Fact]
    public void HeaderValueEncoder_LeavesNonUrlHeadersUnchanged()
    {
        // Arrange
        var contentType = "text/plain; charset=utf-8";

        // Act
        var result = HeaderValueEncoder.Sanitize("Content-Type", contentType);

        // Assert
        Assert.Equal(contentType, result);
    }

    [Fact]
    public void HeaderValueEncoder_EncodesLocationHeader()
    {
        // Arrange
        var location = "/路径/文件.html";

        // Act
        var result = HeaderValueEncoder.Sanitize("Location", location);

        // Assert
        Assert.Equal("/%E8%B7%AF%E5%BE%84/%E6%96%87%E4%BB%B6.html", result);
    }

    [Fact]
    public void HeaderValueEncoder_EncodesContentLocationHeader()
    {
        // Arrange
        var contentLocation = "/内容/位置.txt";

        // Act
        var result = HeaderValueEncoder.Sanitize("Content-Location", contentLocation);

        // Assert
        Assert.Equal("/%E5%86%85%E5%AE%B9/%E4%BD%8D%E7%BD%AE.txt", result);
    }
}