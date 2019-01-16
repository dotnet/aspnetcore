// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Authentication
{
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
}
