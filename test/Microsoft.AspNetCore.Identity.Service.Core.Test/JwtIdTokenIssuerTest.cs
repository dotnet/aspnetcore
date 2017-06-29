// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Identity.Service.Claims;
using Microsoft.AspNetCore.Identity.Service.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class JwtIdTokenIssuerTest
    {
        [Fact]
        public async Task JwtIdTokenIssuer_Fails_IfUserIsMissingUserId()
        {
            // Arrange
            var options = GetOptions();
            var timeManager = GetTimeManager();
            var hasher = GetHasher();

            var issuer = new JwtIdTokenIssuer(GetClaimsManager(), GetSigningPolicy(options, timeManager), new JwtSecurityTokenHandler(), options);
            var context = GetTokenGenerationContext();
            context.InitializeForToken(TokenTypes.IdToken);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => issuer.IssueIdTokenAsync(context));

            // Assert
            Assert.Equal($"Missing '{ClaimTypes.NameIdentifier}' claim from the user.", exception.Message);
        }

        [Fact]
        public async Task JwtIdTokenIssuer_Fails_IfApplicationIsMissingClientId()
        {
            // Arrange
            var options = GetOptions();
            var timeManager = GetTimeManager();

            var hasher = GetHasher();

            var issuer = new JwtIdTokenIssuer(GetClaimsManager(), GetSigningPolicy(options, timeManager), new JwtSecurityTokenHandler(), options);
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })));

            context.InitializeForToken(TokenTypes.IdToken);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => issuer.IssueIdTokenAsync(context));

            // Assert
            Assert.Equal($"Missing '{IdentityServiceClaimTypes.ClientId}' claim from the application.", exception.Message);
        }

        [Fact]
        public async Task JwtIdTokenIssuer_SignsAccessToken()
        {
            // Arrange
            var expectedDateTime = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.FromHours(1));
            var now = DateTimeOffset.UtcNow;
            var expires = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, TimeSpan.Zero);
            var timeManager = GetTimeManager(expectedDateTime, expires, expectedDateTime);

            var hasher = GetHasher();
            var options = GetOptions();

            var handler = new JwtSecurityTokenHandler();

            var tokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = options.Value.SigningKeys[0].Key,
                ValidAudiences = new[] { "clientId" },
                ValidIssuers = new[] { options.Value.Issuer }
            };

            var issuer = new JwtIdTokenIssuer(GetClaimsManager(timeManager), GetSigningPolicy(options, timeManager), new JwtSecurityTokenHandler(), options);
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })));

            context.InitializeForToken(TokenTypes.IdToken);

            // Act
            await issuer.IssueIdTokenAsync(context);

            // Assert
            Assert.NotNull(context.IdToken);
            Assert.NotNull(context.IdToken.SerializedValue);

            SecurityToken validatedToken;
            Assert.NotNull(handler.ValidateToken(context.IdToken.SerializedValue, tokenValidationParameters, out validatedToken));
            Assert.NotNull(validatedToken);

            var jwtToken = Assert.IsType<JwtSecurityToken>(validatedToken);
            var result = Assert.IsType<IdToken>(context.IdToken.Token);
            Assert.Equal("http://www.example.com/issuer", jwtToken.Issuer);
            var tokenAudience = Assert.Single(jwtToken.Audiences);
            Assert.Equal("clientId", tokenAudience);
            Assert.Equal("user", jwtToken.Subject);

            Assert.Equal(expires, jwtToken.ValidTo);
            Assert.Equal(expectedDateTime.UtcDateTime, jwtToken.ValidFrom);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("nonce", null, null)]
        [InlineData("nonce", "code", null)]
        [InlineData("nonce", "code", "accesstoken")]
        [InlineData("nonce", null, "accesstoken")]
        public async Task JwtIdTokenIssuer_IncludesNonceAndTokenHashesWhenPresent(string nonce, string code, string accessToken)
        {
            // Arrange
            var expectedCHash = code != null ? $"#{code}" : null;
            var expectedAtHash = accessToken != null ? $"#{accessToken}" : null;

            var expectedDateTime = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.FromHours(1));
            var expires = DateTimeOffset.UtcNow.AddHours(1);
            var timeManager = GetTimeManager(expectedDateTime, expires, expectedDateTime);

            var options = GetOptions();

            var handler = new JwtSecurityTokenHandler();

            var tokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = options.Value.SigningKeys[0].Key,
                ValidAudiences = new[] { "clientId" },
                ValidIssuers = new[] { options.Value.Issuer }
            };

            var issuer = new JwtIdTokenIssuer(GetClaimsManager(timeManager), GetSigningPolicy(options, timeManager), new JwtSecurityTokenHandler(), options);
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })),
                nonce);

            if (code != null)
            {
                context.InitializeForToken(TokenTypes.AuthorizationCode);
                context.AddToken(new TokenResult(new AuthorizationCode(GetAuthorizationCodeClaims()), "code"));
            }

            if (accessToken != null)
            {
                context.InitializeForToken(TokenTypes.AccessToken);
                context.AddToken(new TokenResult(new AccessToken(GetAccessTokenClaims()), "accesstoken"));
            }

            context.InitializeForToken(TokenTypes.IdToken);

            // Act
            await issuer.IssueIdTokenAsync(context);

            // Assert
            Assert.NotNull(context.IdToken);
            Assert.NotNull(context.IdToken.SerializedValue);

            SecurityToken validatedToken;
            Assert.NotNull(handler.ValidateToken(context.IdToken.SerializedValue, tokenValidationParameters, out validatedToken));
            Assert.NotNull(validatedToken);

            var jwtToken = Assert.IsType<JwtSecurityToken>(validatedToken);
            var result = Assert.IsType<IdToken>(context.IdToken.Token);
            Assert.Equal(nonce, result.Nonce);
            Assert.Equal(nonce, jwtToken.Payload.Nonce);
            Assert.Equal(expectedCHash, result.CodeHash);
            Assert.Equal(expectedCHash, jwtToken.Payload.CHash);
            Assert.Equal(expectedAtHash, result.AccessTokenHash);
            Assert.Equal(expectedAtHash, jwtToken.Payload.Claims.FirstOrDefault(c => c.Type == "at_hash")?.Value);
        }

        private IEnumerable<Claim> GetAccessTokenClaims() =>
            new[]
            {
                new Claim(IdentityServiceClaimTypes.TokenUniqueId,"tokenId"),
                new Claim(IdentityServiceClaimTypes.IssuedAt,"1000"),
                new Claim(IdentityServiceClaimTypes.NotBefore,"1000"),
                new Claim(IdentityServiceClaimTypes.Expires,"1000"),
                new Claim(IdentityServiceClaimTypes.Issuer,"issuer"),
                new Claim(IdentityServiceClaimTypes.Subject,"subject"),
                new Claim(IdentityServiceClaimTypes.Audience,"audience"),
                new Claim(IdentityServiceClaimTypes.AuthorizedParty,"authorizedparty"),
                new Claim(IdentityServiceClaimTypes.Scope,"openid")
            };

        private IEnumerable<Claim> GetAuthorizationCodeClaims() =>
            new[]
            {
                new Claim(IdentityServiceClaimTypes.TokenUniqueId,"tokenId"),
                new Claim(IdentityServiceClaimTypes.IssuedAt,"1000"),
                new Claim(IdentityServiceClaimTypes.NotBefore,"1000"),
                new Claim(IdentityServiceClaimTypes.Expires,"1000"),
                new Claim(IdentityServiceClaimTypes.UserId,"subject"),
                new Claim(IdentityServiceClaimTypes.ClientId,"audience"),
                new Claim(IdentityServiceClaimTypes.Scope,"openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken,"accesstoken"),
                new Claim(IdentityServiceClaimTypes.RedirectUri,"redirectUri"),
            };

        [Fact]
        public async Task JwtIdTokenIssuer_IncludesAllRequiredData()
        {
            // Arrange
            var options = GetOptions();
            var hasher = GetHasher();
            var expectedDateTime = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.FromHours(1));
            var timeManager = GetTimeManager(expectedDateTime, expectedDateTime.AddHours(1), expectedDateTime);
            var issuer = new JwtIdTokenIssuer(GetClaimsManager(timeManager), GetSigningPolicy(options, timeManager), new JwtSecurityTokenHandler(), options);
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })));

            context.InitializeForToken(TokenTypes.IdToken);

            // Act
            await issuer.IssueIdTokenAsync(context);

            // Assert
            Assert.NotNull(context.IdToken);
            var result = Assert.IsType<IdToken>(context.IdToken.Token);
            Assert.NotNull(result);
            Assert.NotNull(result.Id);
            Assert.Equal("user", result.Subject);
            Assert.Equal("clientId", result.Audience);
            Assert.Equal(expectedDateTime, result.IssuedAt);
            Assert.Equal(expectedDateTime.AddHours(1), result.Expires);
            Assert.Equal(expectedDateTime, result.NotBefore);
            Assert.Equal("asdf", result.Nonce);
        }

        private ITokenHasher GetHasher()
        {
            var mock = new Mock<ITokenHasher>();
            mock.Setup(t => t.HashToken("code", "RS256"))
                .Returns("#code");

            mock.Setup(t => t.HashToken("accesstoken", "RS256"))
                .Returns("#accesstoken");

            return mock.Object;
        }

        private TokenGeneratingContext GetTokenGenerationContext(
            ClaimsPrincipal user = null,
            ClaimsPrincipal application = null,
            string nonce = "asdf") =>
            new TokenGeneratingContext(
                user ?? new ClaimsPrincipal(new ClaimsIdentity()),
                application ?? new ClaimsPrincipal(new ClaimsIdentity()),
                new OpenIdConnectMessage
                {
                    Code = "code",
                    Scope = "openid profile",
                    Nonce = nonce,
                    RedirectUri = "http://www.example.com/callback"
                },
                new RequestGrants
                {
                    RedirectUri = "http://www.example.com/callback",
                    Scopes = new[] { ApplicationScope.OpenId, ApplicationScope.Profile },
                    Tokens = new[] { TokenTypes.AuthorizationCode }
                });

        private ITimeStampManager GetTimeManager(
            DateTimeOffset? issuedAt = null,
            DateTimeOffset? expires = null,
            DateTimeOffset? notBefore = null)
        {
            issuedAt = issuedAt ?? DateTimeOffset.Now;
            expires = expires ?? DateTimeOffset.Now;
            notBefore = notBefore ?? DateTimeOffset.Now;

            var manager = new Mock<ITimeStampManager>();

            manager.Setup(m => m.GetCurrentTimeStampInEpochTime())
                .Returns(issuedAt.Value.ToUnixTimeSeconds().ToString());

            manager.SetupSequence(t => t.GetTimeStampInEpochTime(It.IsAny<TimeSpan>()))
                .Returns(notBefore.Value.ToUnixTimeSeconds().ToString())
                .Returns(expires.Value.ToUnixTimeSeconds().ToString());

            manager.Setup(m => m.IsValidPeriod(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .Returns(true);

            return manager.Object;
        }

        private IOptions<IdentityServiceOptions> GetOptions()
        {
            var identityServiceOptions = new IdentityServiceOptions()
            {
                Issuer = "http://www.example.com/issuer"
            };
            var optionsSetup = new IdentityServiceOptionsDefaultSetup();
            optionsSetup.Configure(identityServiceOptions);

            identityServiceOptions.SigningKeys.Add(new SigningCredentials(CryptoUtilities.CreateTestKey(), "RS256"));
            identityServiceOptions.IdTokenOptions.UserClaims.AddSingle("sub", ClaimTypes.NameIdentifier);

            var mock = new Mock<IOptions<IdentityServiceOptions>>();
            mock.Setup(m => m.Value).Returns(identityServiceOptions);

            return mock.Object;
        }

        private ITokenClaimsManager GetClaimsManager(
            ITimeStampManager timeManager = null)
        {
            var options = GetOptions();
            return new DefaultTokenClaimsManager(
                new List<ITokenClaimsProvider>{
                    new DefaultTokenClaimsProvider(options),
                    new GrantedTokensTokenClaimsProvider(),
                    new NonceTokenClaimsProvider(),
                    new ScopesTokenClaimsProvider(),
                    new TimestampsTokenClaimsProvider(timeManager ?? new TimeStampManager(),options),
                    new TokenHashTokenClaimsProvider(GetHasher())
                });
        }

        private ISigningCredentialsPolicyProvider GetSigningPolicy(
            IOptions<IdentityServiceOptions> options,
            ITimeStampManager timeManager)
        {
            var mock = new Mock<IOptionsSnapshot<IdentityServiceOptions>>();
            mock.Setup(m => m.Value).Returns(options.Value);
            mock.Setup(m => m.Get(It.IsAny<string>())).Returns(options.Value);
            return new DefaultSigningCredentialsPolicyProvider(
                new List<ISigningCredentialsSource> {
                            new DefaultSigningCredentialsSource(mock.Object, timeManager)
                },
                timeManager,
                new HostingEnvironment());
        }
    }
}
