// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.Service.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AuthorizationCodeIssuerTest
    {
        [Fact]
        public async Task AuthorizationCodeIssuer_Fails_IfUserIsMissingUserId()
        {
            // Arrange
            var dataFormat = GetDataFormat();
            var issuer = new AuthorizationCodeIssuer(GetClaimsManager(), dataFormat, new ProtocolErrorProvider());
            var context = GetTokenGenerationContext();

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => issuer.CreateAuthorizationCodeAsync(context));

            // Assert
            Assert.Equal($"Missing '{ClaimTypes.NameIdentifier}' claim from the user.", exception.Message);
        }

        [Fact]
        public async Task AuthorizationCodeIssuer_Fails_IfApplicationIsMissingClientId()
        {
            // Arrange
            var dataFormat = GetDataFormat();
            var options = GetOptions();
            var timeManager = GetTimeManager();
            var issuer = new AuthorizationCodeIssuer(GetClaimsManager(), dataFormat, new ProtocolErrorProvider());
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })));

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => issuer.CreateAuthorizationCodeAsync(context));

            // Assert
            Assert.Equal($"Missing '{IdentityServiceClaimTypes.ClientId}' claim from the application.", exception.Message);
        }

        [Fact]
        public async Task AuthorizationCodeIssuer_ProtectsAuthorizationCode()
        {
            // Arrange
            var dataFormat = GetDataFormat();
            var options = GetOptions();
            var timeManager = GetTimeManager();
            var issuer = new AuthorizationCodeIssuer(GetClaimsManager(), dataFormat, new ProtocolErrorProvider());
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })));

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            // Act
            await issuer.CreateAuthorizationCodeAsync(context);

            // Assert
            Assert.NotNull(context.AuthorizationCode);
            Assert.Equal("protected authorization code", context.AuthorizationCode.SerializedValue);
        }

        [Fact]
        public async Task AuthorizationCodeIssuer_IncludesNonceWhenPresent()
        {
            // Arrange
            var dataFormat = GetDataFormat();
            var options = GetOptions();
            var timeManager = GetTimeManager();
            var issuer = new AuthorizationCodeIssuer(GetClaimsManager(), dataFormat, new ProtocolErrorProvider());
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })));

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            // Act
            await issuer.CreateAuthorizationCodeAsync(context);

            // Assert
            Assert.NotNull(context.AuthorizationCode);
            var result = Assert.IsType<AuthorizationCode>(context.AuthorizationCode.Token);
            Assert.NotNull(result);
            Assert.Equal("asdf", result.Nonce);
        }

        [Fact]
        public async Task AuthorizationCodeIssuer_DoesNotIncludeNonceWhenAbsent()
        {
            // Arrange
            var dataFormat = GetDataFormat();
            var issuer = new AuthorizationCodeIssuer(GetClaimsManager(), dataFormat, new ProtocolErrorProvider());
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })),
                nonce: null);

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            // Act
            await issuer.CreateAuthorizationCodeAsync(context);

            // Assert
            Assert.NotNull(context.AuthorizationCode);
            var result = Assert.IsType<AuthorizationCode>(context.AuthorizationCode.Token);
            Assert.NotNull(result);
            Assert.Null(result.Nonce);
        }

        [Fact]
        public async Task AuthorizationCodeIssuer_IncludesAllRequiredData()
        {
            // Arrange
            var dataFormat = GetDataFormat();
            var expectedDateTime = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.FromHours(1));
            var timeManager = GetTimeManager(expectedDateTime, expectedDateTime.AddHours(1), expectedDateTime);

            var issuer = new AuthorizationCodeIssuer(GetClaimsManager(timeManager), dataFormat, new ProtocolErrorProvider());
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })));

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            // Act
            await issuer.CreateAuthorizationCodeAsync(context);

            // Assert
            Assert.NotNull(context.AuthorizationCode);
            var code = Assert.IsType<AuthorizationCode>(context.AuthorizationCode.Token);
            Assert.NotNull(code);
            Assert.NotNull(code.Id);
            Assert.Equal("user", code.UserId);
            Assert.Equal("clientId", code.ClientId);
            Assert.Equal("http://www.example.com/callback", code.RedirectUri);
            Assert.Equal(new[] { "openid" }, code.Scopes);
            Assert.Equal("asdf", code.Nonce);
            Assert.Equal(expectedDateTime, code.IssuedAt);
            Assert.Equal(expectedDateTime.AddHours(1), code.Expires);
            Assert.Equal(expectedDateTime, code.NotBefore);
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
                    Scope = "openid",
                    Nonce = nonce,
                    RedirectUri = "http://www.example.com/callback"
                },
                new RequestGrants
                {
                    Scopes = new ApplicationScope[] { ApplicationScope.OpenId },
                    RedirectUri = "http://www.example.com/callback",
                    Tokens = new string[] { TokenTypes.AuthorizationCode }
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

        private IOptions<IdentityServiceOptions> GetOptions()
        {
            var IdentityServiceOptions = new IdentityServiceOptions();

            var optionsSetup = new IdentityServiceOptionsDefaultSetup();
            optionsSetup.Configure(IdentityServiceOptions);

            var mock = new Mock<IOptions<IdentityServiceOptions>>();
            mock.Setup(m => m.Value).Returns(IdentityServiceOptions);

            return mock.Object;
        }

        private ISecureDataFormat<AuthorizationCode> GetDataFormat()
        {
            var mock = new Mock<ISecureDataFormat<AuthorizationCode>>();
            mock.Setup(s => s.Protect(It.IsAny<AuthorizationCode>()))
                .Returns("protected authorization code");

            return mock.Object;
        }

        private ITokenClaimsManager GetClaimsManager(ITimeStampManager timeManager = null)
        {
            var options = GetOptions();

            return new DefaultTokenClaimsManager(new ITokenClaimsProvider[]
            {
                new DefaultTokenClaimsProvider(options),
                new GrantedTokensTokenClaimsProvider(),
                new NonceTokenClaimsProvider(),
                new ScopesTokenClaimsProvider(),
                new TimestampsTokenClaimsProvider(timeManager ?? new TimeStampManager(), options),
                new TokenHashTokenClaimsProvider(new TokenHasher())
            });
        }
    }
}
