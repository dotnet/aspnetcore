// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenGeneratingContext
    {
        private readonly IList<TokenResult> _issuedTokens = new List<TokenResult>();

        public TokenGeneratingContext(
            ClaimsPrincipal user,
            ClaimsPrincipal application,
            OpenIdConnectMessage requestParameters,
            RequestGrants requestGrants)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (requestParameters == null)
            {
                throw new ArgumentNullException(nameof(requestParameters));
            }

            if (requestGrants == null)
            {
                throw new ArgumentNullException(nameof(requestGrants));
            }

            User = user;
            Application = application;
            RequestParameters = requestParameters;
            RequestGrants = requestGrants;
        }

        public ClaimsPrincipal User { get; }
        public ClaimsPrincipal Application { get; }
        public OpenIdConnectMessage RequestParameters { get; }
        public RequestGrants RequestGrants { get; }
        public IList<Claim> AmbientClaims { get; } = new List<Claim>();
        public string CurrentToken { get; private set; }
        public IList<Claim> CurrentClaims { get; private set; }
        public IEnumerable<TokenResult> IssuedTokens { get => _issuedTokens; }

        public TokenResult AuthorizationCode =>
            IssuedTokens.SingleOrDefault(it => it.Token.IsOfKind(TokenTypes.AuthorizationCode));
        public TokenResult AccessToken =>
            IssuedTokens.SingleOrDefault(it => it.Token.IsOfKind(TokenTypes.AccessToken));
        public TokenResult IdToken =>
            IssuedTokens.SingleOrDefault(it => it.Token.IsOfKind(TokenTypes.IdToken));
        public TokenResult RefreshToken =>
            IssuedTokens.SingleOrDefault(it => it.Token.IsOfKind(TokenTypes.RefreshToken));

        public void InitializeForToken(string tokenType)
        {
            if (tokenType == null)
            {
                throw new ArgumentNullException(nameof(tokenType));
            }

            if (CurrentToken != null)
            {
                throw new InvalidOperationException($"Currently issuing a token for {CurrentToken}");
            }

            if (IssuedTokens.Any(it => it.Token.IsOfKind(tokenType)))
            {
                throw new InvalidOperationException($"A token of type '{tokenType}' has already been emitted.");
            }

            CurrentToken = tokenType;
            CurrentClaims = new List<Claim>();
        }

        public bool IsContextForTokenTypes(params string [] tokenTypes)
        {
            if (CurrentToken == null)
            {
                return false;
            }
            foreach (var token in tokenTypes)
            {
                if (CurrentToken.Equals(token, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddClaimToCurrentToken(Claim claim)
        {
            if (CurrentToken == null)
            {
                throw new InvalidOperationException();
            }

            CurrentClaims.Add(claim);
        }

        public void AddClaimToCurrentToken(string type, string value) => AddClaimToCurrentToken(new Claim(type, value));

        public void AddToken(TokenResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Token.IsOfKind(CurrentToken))
            {
                throw new InvalidOperationException(
                    $"Can't add a result of token type '{result.Token.Kind}' to a context of '{CurrentToken ?? "(null)"}'");
            }

            _issuedTokens.Add(result);
            CurrentToken = null;
            CurrentClaims = null;
        }
    }
}
