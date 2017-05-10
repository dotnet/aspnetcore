// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class LogoutRequestFactory : ILogoutRequestFactory
    {
        private readonly IRedirectUriResolver _redirectUriValidator;
        private readonly ProtocolErrorProvider _errorProvider;

        public LogoutRequestFactory(
            IRedirectUriResolver redirectUriValidator,
            ProtocolErrorProvider errorProvider)
        {
            _redirectUriValidator = redirectUriValidator;
            _errorProvider = errorProvider;
        }

        public async Task<LogoutRequest> CreateLogoutRequestAsync(IDictionary<string, string[]> requestParameters)
        {
            var (state, stateError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.State, _errorProvider);
            if (stateError != null)
            {
                return LogoutRequest.Invalid(stateError);
            }

            var (logoutRedirectUri, redirectUriError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.PostLogoutRedirectUri, _errorProvider);
            if (redirectUriError != null)
            {
                return LogoutRequest.Invalid(redirectUriError);
            }

            var (idTokenHint,idTokenHintError) = RequestParametersHelper.ValidateOptionalParameterIsUnique(requestParameters, OpenIdConnectParameterNames.IdTokenHint, _errorProvider);
            if (idTokenHintError != null)
            {
                return LogoutRequest.Invalid(idTokenHintError);
            }

            var redirectUriValidationResult = await _redirectUriValidator.ResolveLogoutUriAsync(null, logoutRedirectUri);
            if (!redirectUriValidationResult.IsValid)
            {
                return LogoutRequest.Invalid(redirectUriValidationResult.Error);
            }

            return LogoutRequest.Valid(new OpenIdConnectMessage(requestParameters),redirectUriValidationResult.Uri);
        }
    }
}
