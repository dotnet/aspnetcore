// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service.Claims
{
    public class TokenHashTokenClaimsProviderTest
    {
        [Theory]
        [InlineData("access_token", "code")]
        [InlineData("access_token", null)]
        [InlineData(null, "code")]
        [InlineData(null, null)]
        public async Task OnGeneratingClaims_AddsAtHashAndCHashClaimsWhenAvailable(
            string accessToken,
            string code)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage { },
                new RequestGrants { });

            if (accessToken != null)
            {
                context.InitializeForToken(TokenTypes.AccessToken);
                context.AddToken(new TokenResult(new TestToken(TokenTypes.AccessToken), accessToken));
            }
            if (code != null)
            {
                context.InitializeForToken(TokenTypes.AuthorizationCode);
                context.AddToken(new TokenResult(new TestToken(TokenTypes.AuthorizationCode), code));
            }

            // Reference time
            var hasher = new Mock<ITokenHasher>();
            hasher.Setup(h => h.HashToken("access_token", It.IsAny<string>()))
                .Returns("access_token_hash");
            hasher.Setup(h => h.HashToken("code", It.IsAny<string>()))
                .Returns("code_hash");

            var claimsProvider = new TokenHashTokenClaimsProvider(hasher.Object);
            context.InitializeForToken(TokenTypes.IdToken);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            if (accessToken != null)
            {
                Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.AccessTokenHash) && c.Value.Equals("access_token_hash"));
            }
            else
            {
                Assert.DoesNotContain(claims, c => c.Type.Equals(IdentityServiceClaimTypes.AccessTokenHash));
            }

            if (code != null)
            {
                Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.CodeHash) && c.Value.Equals("code_hash"));
            }
            else
            {
                Assert.DoesNotContain(claims, c => c.Type.Equals(IdentityServiceClaimTypes.CodeHash));
            }
        }

        private class TestToken : Token
        {
            private readonly string _kind;

            public TestToken(string kind)
                : base(new List<Claim>
                {
                    new Claim(IdentityServiceClaimTypes.TokenUniqueId, "id"),
                    new Claim(IdentityServiceClaimTypes.IssuedAt, "issuedAt"),
                    new Claim(IdentityServiceClaimTypes.Expires, "expires"),
                    new Claim(IdentityServiceClaimTypes.NotBefore, "notBefore"),
                })
            {
                _kind = kind;
            }

            public override string Kind => _kind;
        }
    }
}
