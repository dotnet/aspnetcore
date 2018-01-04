// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AuthorizationRequestFactoryTest
    {
        public static ProtocolErrorProvider ProtocolErrorProvider = new ProtocolErrorProvider();

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_IfState_HasMultipleValues()
        {
            // Arrange
            var parameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.State] = new[] { "a", "b" }
            };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.TooManyParameters(OpenIdConnectParameterNames.State), null, null);
            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Null(result.Error.RedirectUri);
            Assert.Null(result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_IfClientId_IsMissing()
        {
            // Arrange
            var parameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.State] = new[] { "state" }
            };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.ClientId), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Null(result.Error.RedirectUri);
            Assert.Null(result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_IfMultipleClientIds_ArePresent()
        {
            // Arrange
            var parameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.State] = new[] { "state" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "a", "b" }
            };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.TooManyParameters(OpenIdConnectParameterNames.ClientId), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Null(result.Error.RedirectUri);
            Assert.Null(result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_IfClientIdValidation_Fails()
        {
            // Arrange
            var parameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.State] = new[] { "state" },
                [OpenIdConnectParameterNames.ClientId] = new[] { "a" }
            };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidClientId("a"), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory(validClientId: false);

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Null(result.Error.RedirectUri);
            Assert.Null(result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_IfMultipleRedirectUris_ArePresent()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "a", "b" }
                };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.TooManyParameters(OpenIdConnectParameterNames.RedirectUri), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Null(result.Error.RedirectUri);
            Assert.Null(result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_RedirectUri_IsNotAbsolute()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "/callback" }
                };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidUriFormat("/callback"), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory(validRedirectUri: false);

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Null(result.Error.RedirectUri);
            Assert.Null(result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_RedirectUris_ContainsFragment()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback#fragment" }
                };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidUriFormat("http://www.example.com/callback#fragment"), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory(validRedirectUri: false);

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Null(result.Error.RedirectUri);
            Assert.Null(result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_RedirectUris_IsNotValid()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" }
                };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidRedirectUri("http://www.example.com/callback"), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory(validRedirectUri: false);

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Null(result.Error.RedirectUri);
            Assert.Null(result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_ResponseTypeIsMissing()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" }
                };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.ResponseType), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.Query, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_ResponseType_HasMultipleValues()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code", "token id_token" }
                };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.TooManyParameters(OpenIdConnectParameterNames.ResponseType), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.Query, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_ResponseType_HasInvalidValues()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "invalid" }
                };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidParameterValue("invalid", OpenIdConnectParameterNames.ResponseType), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.Query, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_ResponseType_ContainsOtherValuesAlongWithNone()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code none" }
                };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.ResponseTypeNoneNotAllowed(), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.Fragment, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_ResponseMode_ContainsMultipleValues()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "query", "fragment" }
                };
            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.TooManyParameters(OpenIdConnectParameterNames.ResponseMode), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.Query, result.Error.ResponseMode);
        }

        [Theory]
        [InlineData("code", "query")]
        [InlineData("token", "fragment")]
        [InlineData("id_token", "fragment")]
        [InlineData("code id_token", "fragment")]
        [InlineData("code token", "fragment")]
        [InlineData("code token id_token", "fragment")]
        public async Task FailsToCreateAuthorizationRequest_ResponseMode_ContainsInvalidValues(string responseType, string errorResponseMode)
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { responseType },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "invalid" }
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidParameterValue("invalid", OpenIdConnectParameterNames.ResponseMode), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(errorResponseMode, result.Error.ResponseMode);
        }

        [Theory]
        [InlineData("token", "query")]
        [InlineData("id_token", "query")]
        [InlineData("code id_token", "query")]
        [InlineData("code token", "query")]
        [InlineData("code token id_token", "query")]
        public async Task FailsToCreateAuthorizationRequest_ResponseModeAndResponseType_AreIncompatible(string responseType, string responseMode)
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { responseType },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { responseMode }
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidResponseTypeModeCombination(responseType, responseMode), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.Query, result.Error.ResponseMode);
        }

        [Theory]
        [InlineData("token")]
        [InlineData("id_token")]
        [InlineData("token id_token")]
        [InlineData("code token")]
        [InlineData("code id_token")]
        [InlineData("code token id_token")]
        public async Task FailsToCreateAuthorizationRequest_NonceIsRequired_ForHybridAndImplicitFlows(string responseType)
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { responseType },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" }
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.Nonce), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Theory]
        [InlineData("code")]
        [InlineData("token")]
        [InlineData("id_token")]
        [InlineData("code token")]
        [InlineData("code id_token")]
        [InlineData("token id_token")]
        [InlineData("code token id_token")]
        public async Task FailsToCreateAuthorizationRequest_NonceFails_IfMultipleNoncesArePresent(string responseType)
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { responseType },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                    [OpenIdConnectParameterNames.Nonce] = new[] { "asdf", "qwert" }
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.TooManyParameters(OpenIdConnectParameterNames.Nonce), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task FailsToCreateAuthorizationRequest_IfScope_IsMissingOrEmpty(string scope)
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                };

            if (scope != null)
            {
                parameters[OpenIdConnectParameterNames.Scope] = new[] { scope };
            }

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.Scope), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Theory]
        [InlineData("id_token")]
        [InlineData("code id_token")]
        [InlineData("token id_token")]
        [InlineData("code id_token token")]
        public async Task FailsToCreateAuthorizationRequest_IfRequestAsksForIdToken_ButOpenIdScopeIsMissing(string responseType)
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { responseType },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                    [OpenIdConnectParameterNames.Scope] = new[] { "offline_access" },
                    [OpenIdConnectParameterNames.Nonce] = new[] { "nonce" }
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.MissingOpenIdScope(), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_Scope_HasMultipleValues()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                    [OpenIdConnectParameterNames.Scope] = new[] { "openid", "profile" },
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.TooManyParameters(OpenIdConnectParameterNames.Scope), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_IfScopesResolver_DeterminesThereAreInvalidScopes()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                    [OpenIdConnectParameterNames.Scope] = new[] { "openid invalid" },
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidScope("openid"), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory(validClientId: true, validRedirectUri: true, validScopes: false);

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_Prompt_IncludesNoneAndOtherValues()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                    [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                    [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.Prompt] = new[] { "none consent " }
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.PromptNoneMustBeTheOnlyValue("none consent"), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_Prompt_IncludesUnknownValue()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                    [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                    [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.Prompt] = new[] { "login consent select_account unknown" }
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidPromptValue("unknown"), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Theory]
        [InlineData("none")]
        [InlineData("login")]
        [InlineData("consent")]
        [InlineData("select_account")]
        [InlineData("login consent")]
        [InlineData("login select_account")]
        [InlineData("consent select_account")]
        [InlineData("login consent select_account")]
        public async Task SuccessfullyCreatesARequest_WithAnyValidCombinationOfPromptValues(string promptValues)
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                    [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                    [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                    [OpenIdConnectParameterNames.Prompt] = new[] { promptValues }
                };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidPromptValue("unknown"), null, null);

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_CodeChallenge_HasMultipleValues()
        {
            // Arrange
            var parameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                [OpenIdConnectParameterNames.State] = new[] { "state" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeChallenge] = new[] { "challenge1", "challenge2" }
            };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.TooManyParameters(ProofOfKeyForCodeExchangeParameterNames.CodeChallenge), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Theory]
        [InlineData("tooshort")]
        [InlineData("toolong_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        public async Task FailsToCreateAuthorizationRequest_CodeChallenge_DoesNotHave43Characters(string challenge)
        {
            // Arrange
            var parameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                [OpenIdConnectParameterNames.State] = new[] { "state" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeChallenge] = new[] { challenge }
            };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidCodeChallenge(), "http://www.example.com/callback", "form_post");
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_CodeChallengeMethod_IsMissing()
        {
            // Arrange
            var parameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                [OpenIdConnectParameterNames.State] = new[] { "state" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeChallenge] = new[] { "0123456789012345678901234567890123456789012" }
            };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.MissingRequiredParameter(ProofOfKeyForCodeExchangeParameterNames.CodeChallengeMethod), "http://www.example.com/callback", "form_post");
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_CodeChallengeMethod_HasMultipleValues()
        {
            // Arrange
            var parameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                [OpenIdConnectParameterNames.State] = new[] { "state" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeChallenge] = new[] { "0123456789012345678901234567890123456789012" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeChallengeMethod] = new[] { "S256", "plain" }
            };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.TooManyParameters(ProofOfKeyForCodeExchangeParameterNames.CodeChallengeMethod), "http://www.example.com/callback", "form_post");
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Fact]
        public async Task FailsToCreateAuthorizationRequest_CodeChallengeMethod_IsNotSHA256()
        {
            // Arrange
            var parameters = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                [OpenIdConnectParameterNames.State] = new[] { "state" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeChallenge] = new[] { "0123456789012345678901234567890123456789012" },
                [ProofOfKeyForCodeExchangeParameterNames.CodeChallengeMethod] = new[] { "plain" }
            };

            var expectedError = new AuthorizationRequestError(ProtocolErrorProvider.InvalidCodeChallengeMethod("plain"), null, null);
            expectedError.Message.State = "state";

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(expectedError, result.Error, IdentityServiceErrorComparer.Instance);
            Assert.Equal("http://www.example.com/callback", result.Error.RedirectUri);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.Error.ResponseMode);
        }

        [Fact]
        public async Task CreatesAnAuthorizationRequest_IfAllParameters_AreCorrect()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://www.example.com/callback" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                    [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                    [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                };

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.True(result.IsValid);
            var request = result.Message;
            Assert.NotNull(request);
            Assert.Equal("a", request.ClientId);
            Assert.Equal("http://www.example.com/callback", request.RedirectUri);
            Assert.Equal("code", request.ResponseType);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, request.ResponseMode);
            Assert.Equal("asdf", request.Nonce);
            Assert.Equal("state", request.State);
            Assert.Equal(new[] { ApplicationScope.OpenId, ApplicationScope.Profile }, result.RequestGrants.Scopes);
            Assert.Equal(new[] { TokenTypes.AuthorizationCode }, result.RequestGrants.Tokens);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.RequestGrants.ResponseMode);
            Assert.Equal("http://www.example.com/callback", result.RequestGrants.RedirectUri);
            Assert.Empty(result.RequestGrants.Claims);
        }

        [Fact]
        public async Task CreateAuthorizationRequest_AsignsRegisteredRedirectUriIfMissing()
        {
            // Arrange
            var parameters =
                new Dictionary<string, string[]>
                {
                    [OpenIdConnectParameterNames.ClientId] = new[] { "a" },
                    [OpenIdConnectParameterNames.ResponseType] = new[] { "code" },
                    [OpenIdConnectParameterNames.ResponseMode] = new[] { "form_post" },
                    [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                    [OpenIdConnectParameterNames.Scope] = new[] { "  openid   profile   " },
                    [OpenIdConnectParameterNames.State] = new[] { "state" },
                };

            var factory = CreateAuthorizationRequestFactory();

            // Act
            var result = await factory.CreateAuthorizationRequestAsync(parameters);

            // Assert
            Assert.True(result.IsValid);
            var request = result.Message;
            Assert.NotNull(request);
            Assert.Equal("a", request.ClientId);
            Assert.Null(request.RedirectUri);
            Assert.Equal("code", request.ResponseType);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, request.ResponseMode);
            Assert.Equal("asdf", request.Nonce);
            Assert.Equal("state", request.State);
            Assert.Equal(new[] { ApplicationScope.OpenId, ApplicationScope.Profile }, result.RequestGrants.Scopes);
            Assert.Equal(new[] { TokenTypes.AuthorizationCode }, result.RequestGrants.Tokens);
            Assert.Equal(OpenIdConnectResponseMode.FormPost, result.RequestGrants.ResponseMode);
            Assert.Equal("http://www.example.com/registered", result.RequestGrants.RedirectUri);
            Assert.Empty(result.RequestGrants.Claims);
        }

        private static IAuthorizationRequestFactory CreateAuthorizationRequestFactory(bool validClientId = true, bool validRedirectUri = true, bool validScopes = true)
        {
            var clientIdValidator = new Mock<IClientIdValidator>();
            clientIdValidator
                .Setup(c => c.ValidateClientIdAsync(It.IsAny<string>()))
                .ReturnsAsync(validClientId);

            var redirectUriValidatorMock = new Mock<IRedirectUriResolver>();
            redirectUriValidatorMock
                .Setup(m => m.ResolveRedirectUriAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string clientId, string redirectUrl) => validRedirectUri ?
                    RedirectUriResolutionResult.Valid(redirectUrl ?? "http://www.example.com/registered") :
                    RedirectUriResolutionResult.Invalid(ProtocolErrorProvider.InvalidRedirectUri(redirectUrl)));

            var scopeValidatorMock = new Mock<IScopeResolver>();
            scopeValidatorMock
                .Setup(m => m.ResolveScopesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(
                (string clientId, IEnumerable<string> scopes) => validScopes ?
                    ScopeResolutionResult.Valid(scopes.Select(s => ApplicationScope.CanonicalScopes.TryGetValue(s, out var parsedScope) ? parsedScope : new ApplicationScope(clientId, s))) :
                    ScopeResolutionResult.Invalid(ProtocolErrorProvider.InvalidScope(scopes.First())));

            return new AuthorizationRequestFactory(
                clientIdValidator.Object,
                redirectUriValidatorMock.Object,
                scopeValidatorMock.Object,
                Enumerable.Empty<IAuthorizationRequestValidator>(),
                new ProtocolErrorProvider());
        }
    }
}
