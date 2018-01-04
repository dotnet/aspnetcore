// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdTokenTest
    {
        [Fact]
        public void CreateIdToken_Fails_IfMissingIssuerClaim()
        {
            // Arrange
            var claims = new List<Claim>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfMultipleIssuerClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer,"issuer"),
                new Claim(IdentityServiceClaimTypes.Issuer,"issuer"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfMissingSubjectClaim()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Subject, "subject")
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfMultipleSubjectClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject")
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfMissingAudienceClaim()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereAreMultipleAudienceClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience2"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereAreMultipleNonceClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce2"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereAreMultipleCodeHashClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash2"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereAreMultipleAccessTokenHashClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereIsNoTokenId()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereAreMultipletokenIdClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "id1"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "id2"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereIsNoIssuedAt()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereAreMultipleIssuedAtClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt1"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt2"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereIsNoExpires()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereAreMultipleExpiresClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt"),
                new Claim(IdentityServiceClaimTypes.Expires, "expires"),
                new Claim(IdentityServiceClaimTypes.Expires, "expires"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereIsNoNotBefore()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt"),
                new Claim(IdentityServiceClaimTypes.Expires, "expires"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }

        [Fact]
        public void CreateIdToken_Fails_IfThereAreMultipleNotBeforeClaims()
        {
            // Arrange
            var claims = new List<Claim>()
            {
                new Claim(IdentityServiceClaimTypes.Issuer, "issuer"),
                new Claim(IdentityServiceClaimTypes.Subject, "subject"),
                new Claim(IdentityServiceClaimTypes.Audience, "audience1"),
                new Claim(IdentityServiceClaimTypes.Nonce, "nonce1"),
                new Claim(IdentityServiceClaimTypes.CodeHash, "chash1"),
                new Claim(IdentityServiceClaimTypes.AccessTokenHash, "athash2"),
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, "tuid"),
                new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt"),
                new Claim(IdentityServiceClaimTypes.Expires, "expires"),
                new Claim(IdentityServiceClaimTypes.NotBefore, "notBefore"),
                new Claim(IdentityServiceClaimTypes.NotBefore, "notBefore"),
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new IdToken(claims));
        }
    }
}
