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
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new RedirectHttpResult("url");
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    protected override Task ExecuteAsync(HttpContext httpContext, string contentPath)
    {
        var redirectResult = new RedirectHttpResult(contentPath, false, false);
        return redirectResult.ExecuteAsync(httpContext);
    }
}
