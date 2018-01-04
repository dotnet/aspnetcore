// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AuthorizationRequestFactory : IAuthorizationRequestFactory
    {
        private static readonly string[] ValidResponseTypes = new string[] {
            OpenIdConnectResponseType.None,
            OpenIdConnectResponseType.Token,
            OpenIdConnectResponseType.IdToken,
            OpenIdConnectResponseType.Code
        };

        private static readonly string[] ValidResponseModes = new string[] {
            OpenIdConnectResponseMode.Query,
            OpenIdConnectResponseMode.Fragment,
            OpenIdConnectResponseMode.FormPost
        };

        private readonly IClientIdValidator _clientIdValidator;
        private readonly IRedirectUriResolver _redirectUrlValidator;
        private readonly IScopeResolver _scopeValidator;
        private readonly IEnumerable<IAuthorizationRequestValidator> _validators;
        private readonly ProtocolErrorProvider _errorProvider;

        public AuthorizationRequestFactory(
            IClientIdValidator clientIdValidator,
            IRedirectUriResolver redirectUriValidator,
            IScopeResolver scopeValidator,
            IEnumerable<IAuthorizationRequestValidator> validators,
            ProtocolErrorProvider errorProvider)
        {
            _clientIdValidator = clientIdValidator;
            _redirectUrlValidator = redirectUriValidator;
            _scopeValidator = scopeValidator;
            _validators = validators;
            _errorProvider = errorProvider;
        }

        public async Task<AuthorizationRequest> CreateAuthorizationRequestAsync(IDictionary<string, string[]> requestParameters)
        {
            // Parameters sent without a value MUST be treated as if they were
            // omitted from the request.The authorization server MUST ignore
            // unrecognized request parameters.Request and response parameters
            // MUST NOT be included more than once.

            // Validate that we only got send one state property as it needs to be included in all responses (including error ones)
            var (state, stateError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.State, _errorProvider);


            // Start by validating the client_id and redirect_uri as any of them being invalid indicates that we need to
            // return a 400 response instead of a 302 response with the error. This is signaled by the result not containing
            // a url to redirect to.
            var (clientId, redirectUri, clientError) = await ValidateClientIdAndRedirectUri(requestParameters, state);
            if (clientError != null)
            {
                // Send first the state error if there was one.
                return AuthorizationRequest.Invalid(new AuthorizationRequestError(
                    stateError ?? clientError,
                    redirectUri: null,
                    responseMode: null));
            }

            // We need to determine what response mode to use to send the errors in case there are any.
            // In case the response type and response modes are valid, we should use those values when
            // notifying clients of the errors.
            // In case there is an issue with the response type or the response mode we need to determine
            // how to notify the relying party of the errors.
            // We can divide this in two situations:
            // The response mode is invalid:
            //  * We ignore the response mode and base our response based on the response type specified.
            //      If a token was requested we send the error response on the fragment of the redirect uri.
            //      If no token was requested we send the error response on the query of the redirect uri.
            // The response type is invalid:
            //  * We try to determine if this is a hybrid or implicit flow:
            //      If the invalid response type contained a request for an id_token or an access_token, or
            //      contained more than one space separated value, we send the response on the fragment,
            //      unless the response mode is specified and form_post.
            //      If the invalid response type only contained one value and we can not determine is an
            //      implicit request flow, we return the error on the query string unless the response mode
            //      is specified and form_post or fragment.

            var (responseType, parsedResponseType, tokenRequested, responseTypeError) = ValidateResponseType(requestParameters);
            var (responseMode, responseModeError) = ValidateResponseMode(requestParameters);

            var invalidCombinationError = ValidateResponseModeTypeCombination(responseType, tokenRequested, responseMode);
            if (responseModeError != null || responseMode == null)
            {
                responseMode = GetResponseMode(parsedResponseType, tokenRequested);
            }

            if (responseTypeError != null)
            {
                responseTypeError.State = state;
                return AuthorizationRequest.Invalid(
                    new AuthorizationRequestError(stateError ?? responseTypeError, redirectUri, responseMode));
            }

            if (responseModeError != null)
            {
                responseModeError.State = state;
                return AuthorizationRequest.Invalid(
                    new AuthorizationRequestError(stateError ?? responseModeError, redirectUri, responseMode));
            }

            if (invalidCombinationError != null)
            {
                invalidCombinationError.State = state;
                return AuthorizationRequest.Invalid(
                    new AuthorizationRequestError(stateError ?? invalidCombinationError, redirectUri, responseMode));
            }

            var (nonce, nonceError) = tokenRequested ?
                RequestParametersHelper.ValidateParameterIsUnique(requestParameters, OpenIdConnectParameterNames.Nonce, _errorProvider) :
                RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.Nonce, _errorProvider);

            if (nonceError != null)
            {
                nonceError.State = state;
                return AuthorizationRequest.Invalid(new AuthorizationRequestError(nonceError, redirectUri, responseMode));
            }

            var (scope, scopeError) = RequestParametersHelper.ValidateParameterIsUnique(requestParameters, OpenIdConnectParameterNames.Scope, _errorProvider);
            if (scopeError != null)
            {
                scopeError.State = state;
                return AuthorizationRequest.Invalid(new AuthorizationRequestError(scopeError, redirectUri, responseMode));
            }

            var parsedScope = scope.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var allWhiteSpace = true;
            for (int i = 0; i < parsedScope.Length; i++)
            {
                allWhiteSpace = string.IsNullOrWhiteSpace(parsedScope[i]);
                if (!allWhiteSpace)
                {
                    break;
                }
            }

            if (allWhiteSpace)
            {
                scopeError = _errorProvider.MissingRequiredParameter(OpenIdConnectParameterNames.Scope);
                scopeError.State = state;

                return AuthorizationRequest.Invalid(new AuthorizationRequestError(
                    scopeError,
                    redirectUri,
                    responseMode));
            }

            if (parsedResponseType.Contains(OpenIdConnectResponseType.IdToken) && !parsedScope.Contains(OpenIdConnectScope.OpenId))
            {
                scopeError = _errorProvider.MissingOpenIdScope();
                scopeError.State = state;

                return AuthorizationRequest.Invalid(new AuthorizationRequestError(
                    scopeError,
                    redirectUri,
                    responseMode));
            }

            var resolvedScopes = await _scopeValidator.ResolveScopesAsync(clientId, parsedScope);
            if (!resolvedScopes.IsValid)
            {
                resolvedScopes.Error.State = state;
                return AuthorizationRequest.Invalid(new AuthorizationRequestError(resolvedScopes.Error, redirectUri, responseMode));
            }

            var (prompt, promptError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.Prompt, _errorProvider);
            if (promptError != null)
            {
                promptError.State = state;
                return AuthorizationRequest.Invalid(new AuthorizationRequestError(promptError, redirectUri, responseMode));
            }

            if (prompt != null)
            {
                var parsedPrompt = prompt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                promptError = ValidatePrompt(parsedPrompt);
                if (promptError != null)
                {
                    promptError.State = state;
                    return AuthorizationRequest.Invalid(new AuthorizationRequestError(promptError, redirectUri, responseMode));
                }
            }

            var (codeChallenge, codeChallengeError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, ProofOfKeyForCodeExchangeParameterNames.CodeChallenge, _errorProvider);
            if (codeChallengeError != null)
            {
                codeChallengeError.State = state;
                return AuthorizationRequest.Invalid(new AuthorizationRequestError(codeChallengeError, redirectUri, responseMode));
            }

            if (codeChallenge != null)
            {
                // The code challenge needs to be 43 characters long as its the result of Base64URLEncode(SHA256(code_verifier)).
                // We do this check here because the code challenge might get saved in the serialized authorization code and we
                // want to prevent it from getting unnecessarily big.
                if (codeChallenge.Length != 43)
                {
                    var invalidCodeChallenge = _errorProvider.InvalidCodeChallenge();
                    invalidCodeChallenge.State = state;
                    return AuthorizationRequest.Invalid(new AuthorizationRequestError(
                        invalidCodeChallenge,
                        redirectUri,
                        responseMode));
                }

                var (codeChallengeMethod, codeChallengeMethodError) = RequestParametersHelper.ValidateParameterIsUnique(requestParameters, ProofOfKeyForCodeExchangeParameterNames.CodeChallengeMethod, _errorProvider);
                if (codeChallengeMethodError != null)
                {
                    codeChallengeMethodError.State = state;
                    return AuthorizationRequest.Invalid(new AuthorizationRequestError(codeChallengeMethodError, redirectUri, responseMode));
                }

                if (!codeChallengeMethod.Equals(ProofOfKeyForCodeExchangeChallengeMethods.SHA256, StringComparison.Ordinal))
                {
                    var invalidChallengeMethod = _errorProvider.InvalidCodeChallengeMethod(codeChallengeMethod);
                    invalidChallengeMethod.State = state;
                    return AuthorizationRequest.Invalid(new AuthorizationRequestError(invalidChallengeMethod, redirectUri, responseMode));
                }
            }

            var result = new OpenIdConnectMessage(requestParameters);
            result.RequestType = OpenIdConnectRequestType.Authentication;

            var requestGrants = new RequestGrants
            {
                Tokens = GetRequestedTokens(parsedResponseType, resolvedScopes.Scopes),
                Scopes = resolvedScopes.Scopes.ToList(),
                ResponseMode = responseMode,
                RedirectUri = redirectUri
            };

            return await ValidateRequestAsync(AuthorizationRequest.Valid(result, requestGrants));
        }

        private IList<string> GetRequestedTokens(IEnumerable<string> parsedResponseType, IEnumerable<ApplicationScope> scopes)
        {
            var tokens = new List<string>();
            foreach (var response in parsedResponseType)
            {
                switch (response)
                {
                    case OpenIdConnectResponseType.Code:
                        tokens.Add(TokenTypes.AuthorizationCode);
                        break;
                    case OpenIdConnectResponseType.Token when HasCustomScope():
                        tokens.Add(TokenTypes.AccessToken);
                        break;
                    case OpenIdConnectResponseType.IdToken when HasOpenIdScope():
                        tokens.Add(TokenTypes.IdToken);
                        break;
                    default:
                        break;
                }
            }

            return tokens;

            bool HasCustomScope() => scopes.Any(s => s.ClientId != null);
            bool HasOpenIdScope() => scopes.Contains(ApplicationScope.OpenId);
        }


        private async Task<AuthorizationRequest> ValidateRequestAsync(AuthorizationRequest authorizationRequest)
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

        private OpenIdConnectMessage ValidatePrompt(string[] parsedPrompt)
        {
            for (int i = 0; i < parsedPrompt.Length; i++)
            {
                var prompt = parsedPrompt[i];
                if (string.Equals(prompt, PromptValues.None, StringComparison.Ordinal))
                {
                    if (parsedPrompt.Length > 1)
                    {
                        return _errorProvider.PromptNoneMustBeTheOnlyValue(string.Join(" ", parsedPrompt));
                    }

                    continue;
                }

                if (string.Equals(prompt, PromptValues.Login, StringComparison.Ordinal) ||
                    string.Equals(prompt, PromptValues.Consent, StringComparison.Ordinal) ||
                    string.Equals(prompt, PromptValues.SelectAccount, StringComparison.Ordinal))
                {
                    continue;
                }

                return _errorProvider.InvalidPromptValue(prompt);
            }

            return null;
        }

        private static string GetResponseMode(string[] parsedResponseType, bool tokenRequested)
        {
            return tokenRequested || parsedResponseType != null && parsedResponseType.Length > 1
                                ? OpenIdConnectResponseMode.Fragment : OpenIdConnectResponseMode.Query;
        }

        private OpenIdConnectMessage ValidateResponseModeTypeCombination(string responseType, bool tokenRequested, string responseMode)
        {
            return tokenRequested && responseMode != null && responseMode.Equals(OpenIdConnectResponseMode.Query) ?
                _errorProvider.InvalidResponseTypeModeCombination(responseType, responseMode) :
                null;
        }

        private (string responseMode, OpenIdConnectMessage responseModeError) ValidateResponseMode(IDictionary<string, string[]> parameters)
        {
            var (responseMode, responseModeParameterError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(parameters, OpenIdConnectParameterNames.ResponseMode, _errorProvider);
            var responseModeValidationError = responseMode != null && !ValidResponseModes.Contains(responseMode) ?
                _errorProvider.InvalidParameterValue(responseMode, OpenIdConnectParameterNames.ResponseMode) :
                null;
            var isResponseModeInvalid = responseModeParameterError != null || responseModeValidationError != null;

            return (isResponseModeInvalid ? null : responseMode, responseModeParameterError ?? responseModeValidationError);
        }

        private (string responseType, string[] parsedResponseType, bool tokenRequested, OpenIdConnectMessage error) ValidateResponseType(IDictionary<string, string[]> parameters)
        {
            var (responseType, responseTypeParameterError) = RequestParametersHelper.ValidateParameterIsUnique(parameters, OpenIdConnectParameterNames.ResponseType, _errorProvider);
            var parsedResponseType = responseType?.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var (tokenRequested, responseTypeValidationError) = parsedResponseType != null ? IsValidResponseTypeCombination(parsedResponseType) : (false, null);

            return (responseType, parsedResponseType, tokenRequested, responseTypeParameterError ?? responseTypeValidationError);
        }

        private (bool tokenRequested, OpenIdConnectMessage error) IsValidResponseTypeCombination(string[] parsedResponseType)
        {
            var containsNone = false;
            var tokenRequested = false;
            for (var i = 0; i < parsedResponseType.Length; i++)
            {
                containsNone = containsNone || string.Equals(OpenIdConnectResponseType.None, parsedResponseType[i], StringComparison.Ordinal);

                tokenRequested = tokenRequested ||
                    string.Equals(OpenIdConnectResponseType.Token, parsedResponseType[i], StringComparison.Ordinal) ||
                    string.Equals(OpenIdConnectResponseType.IdToken, parsedResponseType[i], StringComparison.Ordinal);
            }

            if (containsNone && parsedResponseType.Length > 1)
            {
                return (tokenRequested, _errorProvider.ResponseTypeNoneNotAllowed());
            }

            for (var i = 0; i < parsedResponseType.Length; i++)
            {
                if (!ValidResponseTypes.Contains(parsedResponseType[i]))
                {
                    var error = _errorProvider.InvalidParameterValue(
                        parsedResponseType[i],
                        OpenIdConnectParameterNames.ResponseType);
                    return (tokenRequested, error);
                }
            }

            return (tokenRequested, null);

        }

        private async Task<(string clientId, string redirectUri, OpenIdConnectMessage error)> ValidateClientIdAndRedirectUri(
            IDictionary<string, string[]> requestParameters, string state)
        {
            var (clientId, clientIdError) = RequestParametersHelper.ValidateParameterIsUnique(requestParameters, OpenIdConnectParameterNames.ClientId, _errorProvider);
            if (clientIdError != null)
            {
                clientIdError.State = state;
                return (null, null, clientIdError);
            }

            if (!await _clientIdValidator.ValidateClientIdAsync(clientId))
            {
                clientIdError = _errorProvider.InvalidClientId(clientId);
                clientIdError.State = state;

                return (null, null, clientIdError);
            }

            var (redirectUri, redirectUriError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.RedirectUri, _errorProvider);
            if (redirectUriError != null)
            {
                redirectUriError.State = state;
                return (null, null, redirectUriError);
            }

            if (redirectUri != null)
            {
                if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                {
                    redirectUriError = _errorProvider.InvalidUriFormat(redirectUri);
                    redirectUriError.State = state;
                    return (null, null, redirectUriError);
                }

                var parsedUri = new Uri(redirectUri, UriKind.Absolute);
                if (!string.IsNullOrEmpty(parsedUri.Fragment))
                {
                    redirectUriError = _errorProvider.InvalidUriFormat(redirectUri);
                    redirectUriError.State = state;
                    return (null, null, redirectUriError);
                }
            }

            var resolvedUriResult = await _redirectUrlValidator.ResolveRedirectUriAsync(clientId, redirectUri);
            if (!resolvedUriResult.IsValid)
            {
                resolvedUriResult.Error.State = state;
                return (null, null, resolvedUriResult.Error);
            }

            return (clientId, resolvedUriResult.Uri, null);
        }

    }
}
