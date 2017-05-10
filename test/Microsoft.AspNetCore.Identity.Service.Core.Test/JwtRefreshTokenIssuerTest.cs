// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity.Service.Claims;
using Microsoft.AspNetCore.Identity.Service.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class JwtRefreshTokenIssuerTest
    {
        [Fact]
        public async Task JwtRefreshTokenIssuer_Fails_IfUserIsMissingUserId()
        {
            // Arrange
            var dataFormat = GetDataFormat();
            var options = GetOptions();
            var timeManager = GetTimeManager();
            var issuer = new RefreshTokenIssuer(GetClaimsManager(), dataFormat);
            var context = GetTokenGenerationContext();

            context.InitializeForToken(TokenTypes.RefreshToken);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => issuer.IssueRefreshTokenAsync(context));

            // Assert
            Assert.Equal($"Missing '{ClaimTypes.NameIdentifier}' claim from the user.", exception.Message);
        }

        [Fact]
        public async Task JwtRefreshTokenIssuer_Fails_IfApplicationIsMissingClientId()
        {
            // Arrange
            var dataFormat = GetDataFormat();
            var options = GetOptions();
            var timeManager = GetTimeManager();
            var issuer = new RefreshTokenIssuer(GetClaimsManager(), dataFormat);
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })));

            context.InitializeForToken(TokenTypes.RefreshToken);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => issuer.IssueRefreshTokenAsync(context));

            // Assert
            Assert.Equal($"Missing '{IdentityServiceClaimTypes.ClientId}' claim from the application.", exception.Message);
        }

        [Fact]
        public async Task JwtRefreshTokenIssuer_ExchangeRefreshTokenAsync_ReadTheRefreshTokenCorrectly()
        {
            // Arrange
            var options = GetOptions();
            var protector = new EphemeralDataProtectionProvider(new LoggerFactory()).CreateProtector("test");
            var refreshTokenSerializer = new TokenDataSerializer<RefreshToken>(options, ArrayPool<char>.Shared);
            var dataFormat = new SecureDataFormat<RefreshToken>(refreshTokenSerializer, protector);

            var expectedDateTime = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.FromHours(1));
            var now = DateTimeOffset.UtcNow;
            var expires = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, TimeSpan.Zero);
            var timeManager = GetTimeManager(expectedDateTime, expires, expectedDateTime);

            var issuer = new RefreshTokenIssuer(GetClaimsManager(timeManager), dataFormat);
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })));

            context.InitializeForToken(TokenTypes.RefreshToken);

            await issuer.IssueRefreshTokenAsync(context);

            var message = new OpenIdConnectMessage();
            message.ClientId = "clientId";
            message.RefreshToken = context.RefreshToken.SerializedValue;

            // Act
            var grant = await issuer.ExchangeRefreshTokenAsync(message);

            // Assert
            Assert.NotNull(grant);
            Assert.NotNull(grant.Token);
            var refreshToken = Assert.IsType<RefreshToken>(grant.Token);

            Assert.Equal("clientId", refreshToken.ClientId);
            Assert.Equal("user", refreshToken.UserId);

            Assert.Equal(expectedDateTime, refreshToken.IssuedAt);
            Assert.Equal(expires, refreshToken.Expires);
            Assert.Equal(expectedDateTime, refreshToken.NotBefore);

            Assert.Equal(new[] { "openid profile" }, refreshToken.Scopes.ToArray());
        }

        [Fact]
        public async Task JwtRefreshTokenIssuer_IncludesAllRequiredData()
        {
            // Arrange
            var dataFormat = GetDataFormat();
            var options = GetOptions();
            var expectedDateTime = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.FromHours(1));
            var timeManager = GetTimeManager(expectedDateTime, expectedDateTime.AddHours(1), expectedDateTime);
            var issuer = new RefreshTokenIssuer(GetClaimsManager(timeManager), dataFormat);
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })));

            context.InitializeForToken(TokenTypes.RefreshToken);

            // Act
            await issuer.IssueRefreshTokenAsync(context);

            // Assert
            Assert.NotNull(context.RefreshToken);
            var RefreshToken = Assert.IsType<RefreshToken>(context.RefreshToken.Token);
            Assert.NotNull(RefreshToken);
            Assert.NotNull(RefreshToken.Id);
            Assert.Equal("user", RefreshToken.UserId);
            Assert.Equal("clientId", RefreshToken.ClientId);
            Assert.Equal(new[] { "openid profile" }, RefreshToken.Scopes);
            Assert.Equal(expectedDateTime, RefreshToken.IssuedAt);
            Assert.Equal(expectedDateTime.AddHours(1), RefreshToken.Expires);
            Assert.Equal(expectedDateTime, RefreshToken.NotBefore);
        }

        private TokenGeneratingContext GetTokenGenerationContext(
            ClaimsPrincipal user = null,
            ClaimsPrincipal application = null) =>
            new TokenGeneratingContext(
                user ?? new ClaimsPrincipal(new ClaimsIdentity()),
                application ?? new ClaimsPrincipal(new ClaimsIdentity()),
                new OpenIdConnectMessage
                {
                    Code = "code",
                    Scope = "openid profile",
                    Nonce = null,
                    RedirectUri = "http://www.example.com/callback",
                    RequestType = OpenIdConnectRequestType.Token,
                },
                new RequestGrants
                {
                    RedirectUri = "http://www.example.com/callback",
                    Scopes = new ApplicationScope[] { ApplicationScope.OpenId, ApplicationScope.Profile },
                    Tokens = new[] { TokenTypes.AuthorizationCode },
                    Claims = new[] {new Claim("scp","openid profile")}
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

            return manager.Object;
        }

        private ISecureDataFormat<RefreshToken> GetDataFormat()
        {
            var mock = new Mock<ISecureDataFormat<RefreshToken>>();
            mock.Setup(s => s.Protect(It.IsAny<RefreshToken>()))
                .Returns("protected refresh token");

            return mock.Object;
        }

        private IOptions<IdentityServiceOptions> GetOptions()
        {
            var IdentityServiceOptions = new IdentityServiceOptions()
            {
                Issuer = "http://www.example.com/issuer"
            };
            IdentityServiceOptions.SigningKeys.Add(new SigningCredentials(CryptoUtilities.CreateTestKey(), "RS256"));

            var optionsSetup = new IdentityServiceOptionsDefaultSetup();
            optionsSetup.Configure(IdentityServiceOptions);

            var mock = new Mock<IOptions<IdentityServiceOptions>>();
            mock.Setup(m => m.Value).Returns(IdentityServiceOptions);

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
                    new TokenHashTokenClaimsProvider(new TokenHasher())
                });
        }
    }
}
