// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http.Result;

public class RedirectResultTest : RedirectResultTestBase
{
    [Fact]
    public void RedirectResult_Constructor_WithParameterUrlPermanentAndPreservesMethod_SetsResultUrlPermanentAndPreservesMethod()
    {
        // Arrange
        var url = "/test/url";

        // Act
        var result = new RedirectResult(url, permanent: true, preserveMethod: true);

        // Assert
        Assert.True(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.Same(url, result.Url);
    }

    protected override Task ExecuteAsync(HttpContext httpContext, string contentPath)
    {
        var redirectResult = new RedirectResult(contentPath, false, false);
        return redirectResult.ExecuteAsync(httpContext);
    }
}
