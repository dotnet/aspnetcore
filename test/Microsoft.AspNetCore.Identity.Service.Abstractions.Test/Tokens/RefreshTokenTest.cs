// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class RefreshTokenTest
    {
        [Fact]
        public void CreateRefreshToken_Fails_IfMissingUserIdClaim()
        {
            // Arrange
            var claims = new List<Claim>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfMultipleUserClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId,"userId"),
                new Claim(IdentityServiceClaimTypes.UserId,"userId"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfMissingClientIdClaim()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId")
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfMultipleClientIdClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId")
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereIsNoScopeClaim()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereAreMultipleScopeClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid profile"),
                new Claim(IdentityServiceClaimTypes.Scope, "offline_access"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereIsNoGrantedToken()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereIsNoTokenId()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken, "access_token"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereAreMultipletokenIdClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken, "access_token"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "id1"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "id2"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereIsNoIssuedAt()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken, "access_token"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereAreMultipleIssuedAtClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken, "access_token"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt1"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt2"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereIsNoExpires()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken, "access_token"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereAreMultipleExpiresClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken, "access_token"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt"),
                new Claim(IdentityServiceClaimTypes.Expires, "expires"),
                new Claim(IdentityServiceClaimTypes.Expires, "expires"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereIsNoNotBefore()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken, "access_token"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt"),
                new Claim(IdentityServiceClaimTypes.Expires, "expires"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }

        [Fact]
        public void CreateRefreshToken_Fails_IfThereAreMultipleNotBeforeClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.UserId, "userId"),
                new Claim(IdentityServiceClaimTypes.ClientId, "clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "redirectUri1"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken, "access_token"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt"),
                new Claim(IdentityServiceClaimTypes.Expires, "expires"),
                new Claim(IdentityServiceClaimTypes.NotBefore, "notBefore"),
                new Claim(IdentityServiceClaimTypes.NotBefore, "notBefore"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new RefreshToken(claims));
        }
    }
}
