// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service.Claims
{
    public class TokenHashTokenClaimsProvider : ITokenClaimsProvider
    {
        private readonly ITokenHasher _tokenHasher;

        public TokenHashTokenClaimsProvider(ITokenHasher tokenHasher)
        {
            _tokenHasher = tokenHasher;
        }

        public int Order => 100;

        public Task OnGeneratingClaims(TokenGeneratingContext context)
        {
            if (context.IsContextForTokenTypes(TokenTypes.IdToken))
            {
                var accessToken = context
                    .IssuedTokens.SingleOrDefault(t => t.Token.Kind == TokenTypes.AccessToken);
                var authorizationCode = context
                    .IssuedTokens.SingleOrDefault(t => t.Token.Kind == TokenTypes.AuthorizationCode);

                if (accessToken != null)
                {
                    context.CurrentClaims.Add(new Claim(
                        IdentityServiceClaimTypes.AccessTokenHash,
                        GetTokenHash(accessToken.SerializedValue)));
                }

                if (authorizationCode != null)
                {
                    context.CurrentClaims.Add(new Claim(
                        IdentityServiceClaimTypes.CodeHash,
                        GetTokenHash(authorizationCode.SerializedValue)));
                }
            }

            return Task.CompletedTask;
        }

        private string GetTokenHash(string token)
        {
            return _tokenHasher.HashToken(token, "RS256");
        }
    }
}
