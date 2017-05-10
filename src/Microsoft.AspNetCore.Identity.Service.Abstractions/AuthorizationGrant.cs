// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AuthorizationGrant
    {
        private AuthorizationGrant(OpenIdConnectMessage error)
        {
            IsValid = false;
            Error = error;
        }

        private AuthorizationGrant(
            string userId,
            string clientId,
            IEnumerable<string> grantedTokens,
            IEnumerable<ApplicationScope> grantedScopes,
            Token token)
        {
            IsValid = true;
            UserId = userId;
            ClientId = clientId;
            GrantedTokens = grantedTokens;
            GrantedScopes = grantedScopes;
            Token = token;
        }

        public bool IsValid { get; }

        public OpenIdConnectMessage Error { get; }
        public Token Token { get; }
        public string UserId { get; }
        public string ClientId { get; }
        public IEnumerable<string> GrantedTokens { get; }
        public IEnumerable<ApplicationScope> GrantedScopes { get; }
        public string Resource { get; }

        public static AuthorizationGrant Invalid(OpenIdConnectMessage error)
        {
            return new AuthorizationGrant(error);
        }

        public static AuthorizationGrant Valid(
            string userId,
            string clientId,
            IEnumerable<string> grantedTokens,
            IEnumerable<ApplicationScope> grantedScopes,
            Token token)
        {
            return new AuthorizationGrant(userId, clientId, grantedTokens, grantedScopes, token);
        }
    }
}
