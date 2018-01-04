// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenRequestFactory : ITokenRequestFactory
    {
        private static bool[] ValidCodeVerifierCharacters = CreateCodeVerifierValidCharacters();

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
            if (!(consentGrant.Token is AuthorizationCode code))
            {
                throw new InvalidOperationException("Granted token must be an authorization code.");
            }

            var (redirectUri, redirectUriError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.RedirectUri, _errorProvider);
            if (redirectUriError != null)
            {
                return redirectUriError;
            }

            var tokenRedirectUri = code.RedirectUri;
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

            if (code.CodeChallenge != null)
            {
                if (!ProofOfKeyForCodeExchangeChallengeMethods.SHA256.Equals(code.CodeChallengeMethod, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Unsupported code challenge method.");
                }

                var (verifier, verifierError) = RequestParametersHelper.ValidateParameterIsUnique(requestParameters, ProofOfKeyForCodeExchangeParameterNames.CodeVerifier, _errorProvider);
                if (verifierError != null)
                {
                    return verifierError;
                }

                // code-verifier = [a-zA-Z0-9\-._~]{43,128}
                if (verifier.Length < 43 || verifier.Length > 128)
                {
                    return _errorProvider.InvalidCodeVerifier();
                }

                for (var i = 0; i < verifier.Length; i++)
                {
                    if (verifier[i] > 127 || !ValidCodeVerifierCharacters[verifier[i]])
                    {
                        return _errorProvider.InvalidCodeVerifier();
                    }
                }

                if (!string.Equals(code.CodeChallenge, GetComputedChallenge(verifier), StringComparison.Ordinal))
                {
                    return _errorProvider.InvalidCodeVerifier();
                }
            }

            return null;
        }
        private string GetComputedChallenge(string verifier)
        {
            using (var hash = CryptographyHelpers.CreateSHA256())
            {
                return Base64UrlEncoder.Encode(hash.ComputeHash(Encoding.ASCII.GetBytes(verifier)));
            }
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

        // "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ~-._"
        private static bool[] CreateCodeVerifierValidCharacters()
        {
            var result = new bool[128];
            for (var i = 0x41; i <= 0x5A; i++)
            {
                result[i] = true;
            }

            for (var i = 0x61; i <= 0x7A; i++)
            {
                result[i] = true;
            }

            for (var i = 0x30; i <= 0x39; i++)
            {
                result[i] = true;
            }

            result['-'] = true;
            result['.'] = true;
            result['_'] = true;
            result['~'] = true;

            return result;
        }
    }
}
