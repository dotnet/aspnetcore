// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class ClaimUidExtractorTest
    {
        [Fact]
        public void ExtractClaimUid_NullIdentity()
        {
            // Arrange
            IClaimUidExtractor extractor = new DefaultClaimUidExtractor();

            // Act
            var claimUid = extractor.ExtractClaimUid(null);

            // Assert
            Assert.Null(claimUid);
        }

        [Fact]
        public void ExtractClaimUid_Unauthenticated()
        {
            // Arrange
            IClaimUidExtractor extractor = new DefaultClaimUidExtractor();

            var mockIdentity = new Mock<ClaimsIdentity>();
            mockIdentity.Setup(o => o.IsAuthenticated)
                        .Returns(false);

            // Act
            var claimUid = extractor.ExtractClaimUid(mockIdentity.Object);

            // Assert
            Assert.Null(claimUid);
        }

        [Fact]
        public void ExtractClaimUid_ClaimsIdentity()
        {
            // Arrange
            var mockIdentity = new Mock<ClaimsIdentity>();
            mockIdentity.Setup(o => o.IsAuthenticated)
                        .Returns(true);

            IClaimUidExtractor extractor = new DefaultClaimUidExtractor();

            // Act
            var claimUid = extractor.ExtractClaimUid(mockIdentity.Object);

            // Assert
            Assert.NotNull(claimUid);
            Assert.Equal("47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=", claimUid);
        }

        [Fact]
        public void DefaultUniqueClaimTypes_NotPresent_SerializesAllClaimTypes()
        {
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(ClaimTypes.Email, "someone@antifrogery.com"));
            identity.AddClaim(new Claim(ClaimTypes.GivenName, "some"));
            identity.AddClaim(new Claim(ClaimTypes.Surname, "one"));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, String.Empty));

            // Arrange
            var claimsIdentity = (ClaimsIdentity)identity;

            // Act
            var identiferParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(claimsIdentity)
                                                              .ToArray();
            var claims = claimsIdentity.Claims.ToList();
            claims.Sort((a, b) => string.Compare(a.Type, b.Type, StringComparison.Ordinal));

            // Assert
            int index = 0;
            foreach (var claim in claims)
            {
                Assert.True(String.Equals(identiferParameters[index++], claim.Type, StringComparison.Ordinal));
                Assert.True(String.Equals(identiferParameters[index++], claim.Value, StringComparison.Ordinal));
            }
        }

        [Fact]
        public void DefaultUniqueClaimTypes_Present()
        {
            // Arrange
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("fooClaim", "fooClaimValue"));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"));

            // Act
            var uniqueIdentifierParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(identity);

            // Assert
            Assert.Equal(new string[]
            {
                ClaimTypes.NameIdentifier,
                "nameIdentifierValue",
            }, uniqueIdentifierParameters);
        }
    }
}