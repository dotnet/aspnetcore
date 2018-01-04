// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdentityServiceClaimTypes
    {
        public const string TokenUniqueId = "tuid";
        public const string ObjectId = "oid";
        public const string JwtId = "jti";
        public const string Issuer = "iss";
        public const string Subject = "sub";
        public const string Audience = "aud";
        public const string AuthorizedParty = "azp";
        public const string ClientId = "client_id";
        public const string RedirectUri = "r_uri";
        public const string LogoutRedirectUri = "lo_uri";
        public const string IssuedAt = "iat";
        public const string Expires = "exp";
        public const string NotBefore = "nbf";
        public const string Scope = "scp";
        public const string Nonce = "nonce";
        public const string CodeHash = "c_hash";
        public const string AccessTokenHash = "at_hash";
        public const string AuthenticationTime = "auth_time";
        public const string UserId = "user_id";
        public const string Version = "ver";
        public const string Name = "name";
        public const string GrantedToken = "g_token";
        public const string TenantId = "tid";
        public const string Resource = "rid";
        public const string CodeChallenge = "c_chall";
        public const string CodeChallengeMethod = "c_chall_m";
    }
}
