// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenRequestFactoryTest
    {
        public static ProtocolErrorProvider ProtocolErrorProvider = new ProtocolErrorProvider();

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfRequestDoesNotContainGrantType()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                Mock.Of<IRedirectUriResolver>(),
                Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                Mock.Of<ITokenManager>(),
                Mock.Of<ITimeStampManager>(),
                new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.GrantType);

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfGrantTypeIsNotSupported()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.GrantType] = new[] { "unsupported" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                Mock.Of<IRedirectUriResolver>(), Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                Mock.Of<ITokenManager>(),
                Mock.Of<ITimeStampManager>(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidGrantType("unsupported");

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfProvidedGrantIsNotValid()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "invalid" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                Mock.Of<IRedirectUriResolver>(), Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(),
                Mock.Of<ITimeStampManager>(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidGrant();

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfGrantHasExpired()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                Mock.Of<IRedirectUriResolver>(), Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetExpiredToken()),
                Mock.Of<ITimeStampManager>(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidLifetime();

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfClientIdIsMissing()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
            };

            var tokenRequestFactory = new TokenRequestFactory(
                Mock.Of<IClientIdValidator>(),
                Mock.Of<IRedirectUriResolver>(), Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode()),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.ClientId);

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfClientIdDoesntMatchTheClientIdOnTheGrant()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "otherClientId" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                Mock.Of<IRedirectUriResolver>(), Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode()),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidGrant();

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfClientIdIsNotValid()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: false),
                Mock.Of<IRedirectUriResolver>(), Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode()),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidClientId("clientId");

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfClientCredentialsValidationFails()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: false),
                Mock.Of<IRedirectUriResolver>(), Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode()),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidClientCredentials();

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfMultipleScopesArePresent()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.Scope] = new[] { "openid", "profile" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                Mock.Of<IRedirectUriResolver>(),
                Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode()),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.TooManyParameters(OpenIdConnectParameterNames.Scope);

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfRequestContainsInvalidScopes()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.Scope] = new[] { "invalid openid" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                Mock.Of<IRedirectUriResolver>(),
                GetScopeResolver(hasInvalidScopes: true),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode(), null, null, Enumerable.Empty<string>(), new[] { "openid" }),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidScope("invalid");

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfRequestContains_ScopesNotPresentInTheAuthorizationRequest()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.Scope] = new[] { "invalid openid" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                Mock.Of<IRedirectUriResolver>(),
                GetScopeResolver(hasInvalidScopes: false),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode(), null, null, Enumerable.Empty<string>(), new[] { "openid" }),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.UnauthorizedScope();

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfRedirectUriIsNotPresent()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                GetRedirectUriValidator(isRedirectUriValid: false),
                Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode()),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.RedirectUri);

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfCodeVerifierIsMissing()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "https://www.example.com" },
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                GetRedirectUriValidator(isRedirectUriValid: true),
                Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode(new[] {
                    new Claim(IdentityServiceClaimTypes.CodeChallenge,"challenge"),
                    new Claim(IdentityServiceClaimTypes.CodeChallengeMethod, ProofOfKeyForCodeExchangeChallengeMethods.SHA256),
                })),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.MissingRequiredParameter(ProofOfKeyForCodeExchangeParameterNames.CodeVerifier);

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfCodeVerifier_HasMultipleValues()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "https://www.example.com" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeVerifier] = new[] { "value1", "value2" },
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                GetRedirectUriValidator(isRedirectUriValid: true),
                Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode(new[] {
                    new Claim(IdentityServiceClaimTypes.CodeChallenge,"challenge"),
                    new Claim(IdentityServiceClaimTypes.CodeChallengeMethod, ProofOfKeyForCodeExchangeChallengeMethods.SHA256),
                })),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.TooManyParameters(ProofOfKeyForCodeExchangeParameterNames.CodeVerifier);

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfCodeVerifierIsInvalid()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "https://www.example.com" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeVerifier] = new[] { "@" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                GetRedirectUriValidator(isRedirectUriValid: true),
                Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode(new[] {
                    new Claim(IdentityServiceClaimTypes.CodeChallenge,"challenge"),
                    new Claim(IdentityServiceClaimTypes.CodeChallengeMethod, ProofOfKeyForCodeExchangeChallengeMethods.SHA256),
                })),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidCodeVerifier();

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Theory]
        [InlineData("tooShort")]
        [InlineData("tooLooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong")]
        public async Task CreateTokenRequestAsyncFails_IfTooShortOrTooLong(string verifier)
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "https://www.example.com" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeVerifier] = new[] { verifier }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                GetRedirectUriValidator(isRedirectUriValid: true),
                Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode(new[] {
                    new Claim(IdentityServiceClaimTypes.CodeChallenge,"challenge"),
                    new Claim(IdentityServiceClaimTypes.CodeChallengeMethod, ProofOfKeyForCodeExchangeChallengeMethods.SHA256),
                })),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidCodeVerifier();

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfCodeVerifierDoesNotMatchChallenge()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "https://www.example.com" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeVerifier] = new[] { "0123456789012345678901234567890123456789012" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                GetRedirectUriValidator(isRedirectUriValid: true),
                Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode(new[] {
                    new Claim(IdentityServiceClaimTypes.CodeChallenge,"challenge"),
                    new Claim(IdentityServiceClaimTypes.CodeChallengeMethod, ProofOfKeyForCodeExchangeChallengeMethods.SHA256),
                })),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.InvalidCodeVerifier();

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        [Fact]
        public async Task CreateTokenRequestSucceeds_IfCodeVerifier_MatchesChallenge()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "https://www.example.com" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeVerifier] = new[] { "0123456789012345678901234567890123456789012" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                GetRedirectUriValidator(isRedirectUriValid: true),
                Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode(new[] {
                    new Claim(IdentityServiceClaimTypes.CodeChallenge,"_RpfHqw8pAZIomzVUE7sjRmHSM543WVdC4o-Kc4_3C0"),
                    new Claim(IdentityServiceClaimTypes.CodeChallengeMethod, ProofOfKeyForCodeExchangeChallengeMethods.SHA256),
                })),
                new TimeStampManager(), new ProtocolErrorProvider());

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.True(tokenRequest.IsValid);
        }

        private IRedirectUriResolver GetRedirectUriValidator(bool isRedirectUriValid)
        {
            var mock = new Mock<IRedirectUriResolver>();
            mock.Setup(m => m.ResolveRedirectUriAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string clientId, string redirectUri) => isRedirectUriValid ?
                RedirectUriResolutionResult.Valid(redirectUri) :
                RedirectUriResolutionResult.Invalid(ProtocolErrorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.RedirectUri)));

            return mock.Object;
        }

        [Fact]
        public async Task CreateTokenRequestAsyncFails_IfRedirectUriDoesNotMatch()
        {
            // Arrange
            var requestParameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.GrantType] = new[] { "authorization_code" },
                [OpenIdConnectParameterNames.Code] = new[] { "valid" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" }
            };

            var tokenRequestFactory = new TokenRequestFactory(
                GetClientIdValidator(isClientIdValid: true, areClientCredentialsValid: true),
                Mock.Of<IRedirectUriResolver>(), Mock.Of<IScopeResolver>(),
                Enumerable.Empty<ITokenRequestValidator>(),
                GetTestTokenManager(GetValidAuthorizationCode()),
                new TimeStampManager(), new ProtocolErrorProvider());

            var expectedError = ProtocolErrorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.RedirectUri);

            // Act
            var tokenRequest = await tokenRequestFactory.CreateTokenRequestAsync(requestParameters);

            // Assert
            Assert.NotNull(tokenRequest);
            Assert.False(tokenRequest.IsValid);
            Assert.Equal(expectedError, tokenRequest.Error, IdentityServiceErrorComparer.Instance);
        }

        private IClientIdValidator GetClientIdValidator(bool isClientIdValid = false, bool areClientCredentialsValid = false)
        {
            var clientIdValidator = new Mock<IClientIdValidator>();

            clientIdValidator
                .Setup(cv => cv.ValidateClientIdAsync(It.IsAny<string>()))
                .ReturnsAsync(isClientIdValid);

            clientIdValidator
                .Setup(cv => cv.ValidateClientCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(areClientCredentialsValid);

            return clientIdValidator.Object;
        }

        private Token GetValidAuthorizationCode(IEnumerable<Claim> additionalClaims = null)
        {
            var notBefore = EpochTime.GetIntDate(DateTime.UtcNow - TimeSpan.FromMinutes(20)).ToString();
            var expires = EpochTime.GetIntDate(DateTime.UtcNow + TimeSpan.FromMinutes(10)).ToString();
            var issuedAt = EpochTime.GetIntDate(DateTime.UtcNow).ToString();

            return new AuthorizationCode(new Claim[]
            {
                new Claim(IdentityServiceClaimTypes.TokenUniqueId, Guid.NewGuid().ToString()),
                new Claim(IdentityServiceClaimTypes.NotBefore,notBefore),
                new Claim(IdentityServiceClaimTypes.Expires,expires),
                new Claim(IdentityServiceClaimTypes.IssuedAt,issuedAt),
                new Claim(IdentityServiceClaimTypes.UserId,"userId"),
                new Claim(IdentityServiceClaimTypes.ClientId,"clientId"),
                new Claim(IdentityServiceClaimTypes.RedirectUri, "https://www.example.com"),
                new Claim(IdentityServiceClaimTypes.Scope, "openid"),
                new Claim(IdentityServiceClaimTypes.GrantedToken, "id_token")
            }
            .Concat(additionalClaims ?? Enumerable.Empty<Claim>()));
        }

        private ITokenManager GetTestTokenManager(
            Token token = null,
            string userId = null,
            string clientId = null,
            IEnumerable<string> grantedTokens = null,
            IEnumerable<string> grantedScopes = null)
        {
            userId = userId ?? "userId";
            clientId = clientId ?? "clientId";
            grantedTokens = grantedTokens ?? Enumerable.Empty<string>();
            var parsedScopes = grantedScopes?.Select(s => ApplicationScope.CanonicalScopes.TryGetValue(s, out var found) ? found : new ApplicationScope(clientId, s)) ??
                new[] { ApplicationScope.OpenId, ApplicationScope.OfflineAccess };

            var mock = new Mock<ITokenManager>();
            if (token == null)
            {
                mock.Setup(m => m.ExchangeTokenAsync(It.IsAny<OpenIdConnectMessage>()))
                    .ReturnsAsync(AuthorizationGrant.Invalid(ProtocolErrorProvider.InvalidGrant()));
            }
            else
            {
                mock.Setup(m => m.ExchangeTokenAsync(It.IsAny<OpenIdConnectMessage>()))
                    .ReturnsAsync(AuthorizationGrant.Valid(
                        userId,
                        clientId,
                        grantedTokens,
                        parsedScopes,
                        token));
            }

            return mock.Object;
        }

        private Token GetExpiredToken()
        {
            var notBefore = EpochTime.GetIntDate(DateTime.UtcNow - TimeSpan.FromMinutes(20)).ToString();
            var expires = EpochTime.GetIntDate(DateTime.UtcNow - TimeSpan.FromMinutes(10)).ToString();

            return new TestToken(new Claim[]
            {
                new Claim(IdentityServiceClaimTypes.TokenUniqueId,Guid.NewGuid().ToString()),
                new Claim(IdentityServiceClaimTypes.IssuedAt,"946684800"), // 01/01/2000
                new Claim(IdentityServiceClaimTypes.NotBefore,notBefore),
                new Claim(IdentityServiceClaimTypes.Expires,expires)
            });
        }

        private IScopeResolver GetScopeResolver(bool hasInvalidScopes)
        {
            var mock = new Mock<IScopeResolver>();
            mock.Setup(m => m.ResolveScopesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync((string clientId, IEnumerable<string> scopes) => !hasInvalidScopes ?
                ScopeResolutionResult.Valid(scopes.Select(s => CreateScope(s))) :
                ScopeResolutionResult.Invalid(ProtocolErrorProvider.InvalidScope(scopes.First())));

            return mock.Object;

            ApplicationScope CreateScope(string s) =>
                ApplicationScope.CanonicalScopes.TryGetValue(s, out var parsedScope) ? parsedScope : new ApplicationScope("resourceId", s);
        }

        private class TestToken : Token
        {
            public TestToken(IEnumerable<Claim> claims)
                : base(claims)
            {
            }

            public override string Kind => "Test";
        }
    }
}
