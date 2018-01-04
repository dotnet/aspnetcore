// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AuthorizationCodeIssuer : IAuthorizationCodeIssuer
    {
        private readonly ISecureDataFormat<AuthorizationCode> _dataFormat;
        private readonly ITokenClaimsManager _claimsManager;
        private readonly ProtocolErrorProvider _errorProvider;

        public AuthorizationCodeIssuer(
            ITokenClaimsManager claimsManager,
            ISecureDataFormat<AuthorizationCode> dataFormat,
            ProtocolErrorProvider errorProvider)
        {
            _claimsManager = claimsManager;
            _dataFormat = dataFormat;
            _errorProvider = errorProvider;
        }

        public async Task CreateAuthorizationCodeAsync(TokenGeneratingContext context)
        {
            await _claimsManager.CreateClaimsAsync(context);
            var claims = context.CurrentClaims;

            var code = new AuthorizationCode(claims);

            var tokenResult = new TokenResult(code, _dataFormat.Protect(code));

            context.AddToken(tokenResult);
        }

        public Task<AuthorizationGrant> ExchangeAuthorizationCodeAsync(OpenIdConnectMessage message)
        {
            var code = _dataFormat.Unprotect(message.Code);

            if (code == null)
            {
                return Task.FromResult(AuthorizationGrant.Invalid(_errorProvider.InvalidAuthorizationCode()));
            }

            var userId = code.UserId;
            var clientId = code.ClientId;
            var scopes = code.Scopes;
            var resource = code.Resource;
            var nonce = code.Nonce;

            var tokenTypes = code.GrantedTokens;
            var grantedScopes = scopes.SelectMany(s => s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(s => ApplicationScope.CanonicalScopes.TryGetValue(s, out var canonicalScope) ? canonicalScope : new ApplicationScope(resource, s))
                .ToList();

            return Task.FromResult(AuthorizationGrant.Valid(userId, clientId, tokenTypes, grantedScopes, code));
        }
    }
}