// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenRequest
    {
        private TokenRequest(OpenIdConnectMessage error)
        {
            IsValid = false;
            Error = error;
        }

        private TokenRequest(
            OpenIdConnectMessage request,
            string userId,
            string clientId,
            RequestGrants grants)
        {
            IsValid = true;
            Request = request;
            UserId = userId;
            ClientId = clientId;
            RequestGrants = grants;
        }

        public bool IsValid { get; }
        public OpenIdConnectMessage Request { get; }
        public string UserId { get; }
        public string ClientId { get; }
        public OpenIdConnectMessage Error { get; }
        public RequestGrants RequestGrants { get; }

        public static TokenRequest Invalid(OpenIdConnectMessage error)
        {
            return new TokenRequest(error);
        }

        public static TokenRequest Valid(
            OpenIdConnectMessage request,
            string userId,
            string clientId,
            RequestGrants grants)
        {
            return new TokenRequest(request, userId, clientId, grants);
        }

        public TokenGeneratingContext CreateTokenGeneratingContext(ClaimsPrincipal user, ClaimsPrincipal application)
        {
            return new TokenGeneratingContext(user, application, Request, RequestGrants);
        }
    }
}
