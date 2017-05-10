// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service.Claims
{
    public class PairwiseSubClaimProvider : ITokenClaimsProvider
    {
        private readonly IdentityOptions _options;

        public PairwiseSubClaimProvider(IOptions<IdentityOptions> options)
        {
            _options = options.Value;
        }
        public int Order => 200;

        public Task OnGeneratingClaims(TokenGeneratingContext context)
        {
            if(context.CurrentToken.Equals(TokenTypes.IdToken) ||
               context.CurrentToken.Equals(TokenTypes.AccessToken))
            {
                var userId = context.User.FindFirstValue(_options.ClaimsIdentity.UserIdClaimType);
                var applicationId = context.Application.FindFirstValue(IdentityServiceClaimTypes.ObjectId);
                var unHashedSubjectBits = Encoding.ASCII.GetBytes($"{userId}/{applicationId}");
                var hashing = CryptographyHelpers.CreateSHA256();
                var subject = Base64UrlEncoder.Encode(hashing.ComputeHash(unHashedSubjectBits));
                Claim existingClaim = null;
                foreach (var claim in context.CurrentClaims)
                {
                    if (claim.Type.Equals(IdentityServiceClaimTypes.Subject,StringComparison.Ordinal))
                    {
                        existingClaim = claim;
                    }
                }

                if (existingClaim != null)
                {
                    context.CurrentClaims.Remove(existingClaim);
                }

                context.CurrentClaims.Add(new Claim(IdentityServiceClaimTypes.Subject, subject));
            }

            return Task.CompletedTask;
        }
    }
}
