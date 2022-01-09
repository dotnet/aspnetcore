// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Authentication;

public class LocalApiPolicySchemeForwardSelectorTests
{
    [Theory]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Error")]
    [InlineData("/identity/Account/Manage")]
    [InlineData("/Identity/ACCOUNT/TwoFactor")]
    public void SelectScheme_ReturnsTheIdentityApplicationScheme_ForIdentityRelatedPaths(string path)
    {
        // Arrange
        var selector = new IdentityServerJwtPolicySchemeForwardSelector("/Identity", "Local");
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;

        // Act
        var scheme = selector.SelectScheme(ctx);

        // Assert
        Assert.Equal(IdentityConstants.ApplicationScheme, scheme);
    }

    [Theory]
    [InlineData("/api/values")]
    [InlineData("/connect/openid")]
    public void SelectScheme_ReturnsTheDefaultScheme_ForOtherPaths(string path)
    {
        // Arrange
        var selector = new IdentityServerJwtPolicySchemeForwardSelector("/Identity", "Local");
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;

        // Act
        var scheme = selector.SelectScheme(ctx);

        // Assert
        Assert.Equal("Local", scheme);
    }
}
