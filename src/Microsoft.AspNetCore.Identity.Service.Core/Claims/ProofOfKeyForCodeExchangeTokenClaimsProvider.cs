// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Service.Claims;

namespace Microsoft.AspNetCore.Identity.Service.Core.Claims
{
    public class ProofOfKeyForCodeExchangeTokenClaimsProvider : ITokenClaimsProvider
    {
        public int Order => 100;

        public Task OnGeneratingClaims(TokenGeneratingContext context)
        {
            if(context.IsContextForTokenTypes(TokenTypes.AuthorizationCode) &&
                context.RequestParameters.Parameters.ContainsKey(ProofOfKeyForCodeExchangeParameterNames.CodeChallenge))
            {
                context.AddClaimToCurrentToken(
                    IdentityServiceClaimTypes.CodeChallenge,
                    context.RequestParameters.Parameters[ProofOfKeyForCodeExchangeParameterNames.CodeChallenge]);

                context.AddClaimToCurrentToken(
                    IdentityServiceClaimTypes.CodeChallengeMethod,
                    context.RequestParameters.Parameters[ProofOfKeyForCodeExchangeParameterNames.CodeChallengeMethod]);
            }

            return Task.CompletedTask;
        }
    }
}
