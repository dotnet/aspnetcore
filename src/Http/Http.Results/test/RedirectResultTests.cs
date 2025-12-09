// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class RedirectResultTests : RedirectResultTestBase
{
    [Fact]
    public void RedirectResult_Constructor_WithParameterUrlPermanentAndPreservesMethod_SetsResultUrlPermanentAndPreservesMethod()
    {
        // Arrange
        var url = "/test/url";

        // Act
        var result = new RedirectHttpResult(url, permanent: true, preserveMethod: true);

        // Assert
        Assert.True(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new RedirectHttpResult("url");
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/test/path")]
    [InlineData("/test/path?foo=bar#baz")]
    [InlineData("~/")]
    [InlineData("~/Home/About")]
    public void IsLocalUrl_True_ForLocalUrl(string url)
    {
        // Act
        var actual = RedirectHttpResult.IsLocalUrl(url);

        // Assert
        Assert.True(actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("//")]
    [InlineData("/\\")]
    [InlineData("//foo")]
    [InlineData("/\\foo")]
    [InlineData("Home/About")]
    [InlineData("test/path")]
    [InlineData("http://www.example.com")]
    [InlineData("https://example.com/non-local-url/example")]
    [InlineData("https://example.com/non-local-url/example?foo=bar#baz")]
    public void IsLocalUrl_False_ForNonLocalUrl(string url)
    {
        // Act
        var actual = RedirectHttpResult.IsLocalUrl(url);

        // Assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData("~//")]
    [InlineData("~/\\")]
    [InlineData("~//foo")]
    [InlineData("~/\\foo")]
    public void IsLocalUrl_False_ForNonLocalUrlTilde(string url)
    {
        // Act
        var actual = RedirectHttpResult.IsLocalUrl(url);

        // Assert
        Assert.False(actual);
    }

    protected override Task ExecuteAsync(HttpContext httpContext, string contentPath)
    {
        var redirectResult = new RedirectHttpResult(contentPath, false, false);
        return redirectResult.ExecuteAsync(httpContext);
    }
}
