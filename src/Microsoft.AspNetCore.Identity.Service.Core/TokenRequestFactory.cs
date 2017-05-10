// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenRequestFactory : ITokenRequestFactory
    {
        private readonly IClientIdValidator _clientIdValidator;
        private readonly ITokenManager _tokenManager;
        private readonly IRedirectUriResolver _redirectUriValidator;
        private readonly IScopeResolver _scopeResolver;
        private readonly ITimeStampManager _timeStampManager;
        private readonly IEnumerable<ITokenRequestValidator> _validators;
        private readonly ProtocolErrorProvider _errorProvider;

        public TokenRequestFactory(
            IClientIdValidator clientIdValidator,
            IRedirectUriResolver redirectUriValidator,
            IScopeResolver scopeResolver,
            IEnumerable<ITokenRequestValidator> validators,
            ITokenManager tokenManager,
            ITimeStampManager timeStampManager,
            ProtocolErrorProvider errorProvider)
        {
            _clientIdValidator = clientIdValidator;
            _redirectUriValidator = redirectUriValidator;
            _scopeResolver = scopeResolver;
            _validators = validators;
            _tokenManager = tokenManager;
            _errorProvider = errorProvider;
            _timeStampManager = timeStampManager;
        }

        public async Task<TokenRequest> CreateTokenRequestAsync(IDictionary<string, string[]> requestParameters)
        {
            var (clientId, clientIdParameterError) = RequestParametersHelper.ValidateParameterIsUnique(requestParameters, OpenIdConnectParameterNames.ClientId, _errorProvider);
            if (clientIdParameterError != null)
            {
                return TokenRequest.Invalid(clientIdParameterError);
            }

            if (!await _clientIdValidator.ValidateClientIdAsync(clientId))
            {
                return TokenRequest.Invalid(_errorProvider.InvalidClientId(clientId));
            }

            var (clientSecret, clientSecretParameterError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.ClientSecret, _errorProvider);
            if (clientSecretParameterError != null)
            {
                return TokenRequest.Invalid(clientSecretParameterError);
            }

            if (!await _clientIdValidator.ValidateClientCredentialsAsync(clientId, clientSecret))
            {
                return TokenRequest.Invalid(_errorProvider.InvalidClientCredentials());
            }

            var (grantType, grantTypeError) = RequestParametersHelper.ValidateParameterIsUnique(
                requestParameters,
                OpenIdConnectParameterNames.GrantType,
                _errorProvider);

            if (grantTypeError != null)
            {
                return TokenRequest.Invalid(grantTypeError);
            }

            var grantTypeParameter = GetGrantTypeParameter(requestParameters, grantType);
            if (grantTypeParameter == null)
            {
                return TokenRequest.Invalid(_errorProvider.InvalidGrantType(grantType));
            }

            var (grantValue, grantValueError) = RequestParametersHelper.ValidateParameterIsUnique(
                requestParameters,
                grantTypeParameter,
                _errorProvider);

            if (grantValueError != null)
            {
                return TokenRequest.Invalid(clientIdParameterError);
            }

            var message = new OpenIdConnectMessage(requestParameters)
            {
                RequestType = OpenIdConnectRequestType.Token
            };

            // TODO: File a bug to track we might want to redesign this if we want to consider other flows like
            // client credentials or resource owner credentials.
            var consentGrant = await _tokenManager.ExchangeTokenAsync(message);
            if (!consentGrant.IsValid)
            {
                return TokenRequest.Invalid(consentGrant.Error);
            }

            if (!_timeStampManager.IsValidPeriod(consentGrant.Token.NotBefore, consentGrant.Token.Expires))
            {
                return TokenRequest.Invalid(_errorProvider.InvalidLifetime());
            }

            if (!string.Equals(clientId, consentGrant.ClientId, StringComparison.Ordinal))
            {
                return TokenRequest.Invalid(_errorProvider.InvalidGrant());
            }

            var (scope, requestScopesError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.Scope, _errorProvider);
            if (requestScopesError != null)
            {
                return TokenRequest.Invalid(requestScopesError);
            }

            var grantedScopes = consentGrant.GrantedScopes;

            var parsedScope = scope?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parsedScope != null)
            {
                var scopeResolutionResult = await _scopeResolver.ResolveScopesAsync(clientId, parsedScope);
                if (!scopeResolutionResult.IsValid)
                {
                    return TokenRequest.Invalid(scopeResolutionResult.Error);
                }

                if (grantType.Equals(OpenIdConnectGrantTypes.AuthorizationCode, StringComparison.Ordinal) ||
                    grantType.Equals(OpenIdConnectGrantTypes.RefreshToken, StringComparison.Ordinal))
                {
                    if (scopeResolutionResult.Scopes.Any(rs => !consentGrant.GrantedScopes.Contains(rs)))
                    {
                        return TokenRequest.Invalid(_errorProvider.UnauthorizedScope());
                    }
                }

                grantedScopes = scopeResolutionResult.Scopes;
            }

            if (grantType.Equals(OpenIdConnectGrantTypes.AuthorizationCode, StringComparison.Ordinal))
            {
                var authorizationCodeError = await ValidateAuthorizationCode(
                    requestParameters,
                    clientId,
                    consentGrant);

                if (authorizationCodeError != null)
                {
                    return TokenRequest.Invalid(authorizationCodeError);
                }
            }

            var requestGrants = new RequestGrants
            {
                Tokens = consentGrant.GrantedTokens.ToList(),
                Claims = consentGrant.Token.ToList(),
                Scopes = grantedScopes.ToList()
            };

            return await ValidateRequestAsync(TokenRequest.Valid(
                message,
                consentGrant.UserId,
                consentGrant.ClientId,
                requestGrants));
        }

        private async Task<TokenRequest> ValidateRequestAsync(TokenRequest authorizationRequest)
        {
            foreach (var validator in _validators)
            {
                var newRequest = await validator.ValidateRequestAsync(authorizationRequest);
                if (!newRequest.IsValid)
                {
                    return newRequest;
                }
            }

            return authorizationRequest;
        }

        private async Task<OpenIdConnectMessage> ValidateAuthorizationCode(
            IDictionary<string, string[]> requestParameters,
            string clientId,
            AuthorizationGrant consentGrant)
        {
            var (redirectUri, redirectUriError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.RedirectUri, _errorProvider);
            if (redirectUriError != null)
            {
                return redirectUriError;
            }

            var tokenRedirectUri = consentGrant
                .Token.SingleOrDefault(c =>
                    string.Equals(c.Type, IdentityServiceClaimTypes.RedirectUri, StringComparison.Ordinal))?.Value;

            if (redirectUri == null && tokenRedirectUri != null)
            {
                return _errorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.RedirectUri);
            }

            if (!string.Equals(redirectUri, tokenRedirectUri, StringComparison.Ordinal))
            {
                return _errorProvider.MismatchedRedirectUrl(redirectUri);
            }

            var resolution = await _redirectUriValidator.ResolveRedirectUriAsync(clientId, redirectUri);
            if (!resolution.IsValid)
            {
                return _errorProvider.InvalidRedirectUri(redirectUri);
            }

            return null;
        }

        private string GetGrantTypeParameter(IDictionary<string, string[]> parameters, string grantType)
        {
            switch (grantType)
            {
                case OpenIdConnectGrantTypes.AuthorizationCode:
                    return OpenIdConnectParameterNames.Code;
                case OpenIdConnectGrantTypes.RefreshToken:
                    return OpenIdConnectParameterNames.RefreshToken;
                default:
                    return null;
            }
        }
    }
}
