// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AuthorizationRequest
    {
        private AuthorizationRequest(AuthorizationRequestError error)
        {
            IsValid = false;
            Error = error;
        }

        private AuthorizationRequest(OpenIdConnectMessage request, RequestGrants requestGrants)
        {
            IsValid = true;
            Message = request;
            RequestGrants = requestGrants;
        }

        public bool IsValid { get; }
        public AuthorizationRequestError Error { get; }
        public OpenIdConnectMessage Message { get; }
        public RequestGrants RequestGrants { get; }

        public static AuthorizationRequest Invalid(AuthorizationRequestError authorizationRequestError)
        {
            return new AuthorizationRequest(authorizationRequestError);
        }

        public static AuthorizationRequest Valid(
            OpenIdConnectMessage request,
            RequestGrants requestGrants)
        {
            return new AuthorizationRequest(request, requestGrants);
        }

        public TokenGeneratingContext CreateTokenGeneratingContext(ClaimsPrincipal user, ClaimsPrincipal application)
        {
            return new TokenGeneratingContext(
                user,
                application,
                Message,
                RequestGrants);
        }
    }
}
