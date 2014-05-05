// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security
{
    public class DefaultAuthorizationService : IAuthorizationService
    {
        private readonly IList<IAuthorizationPolicy> _policies;
        public int MaxRetries = 99;

        public DefaultAuthorizationService(IEnumerable<IAuthorizationPolicy> policies)
        {
            if (policies == null)
            {
                _policies = Enumerable.Empty<IAuthorizationPolicy>().ToArray();
            } 
            else 
            {
                _policies = policies.OrderBy(x => x.Order).ToArray();
            }
        }

        public async Task<bool> AuthorizeAsync(IEnumerable<Claim> claims, ClaimsPrincipal user)
        {
            return await AuthorizeAsync(claims, user, null);
        }

        public bool Authorize(IEnumerable<Claim> claims, ClaimsPrincipal user)
        {
            return  AuthorizeAsync(claims, user, null).Result;
        }

        public async Task<bool> AuthorizeAsync(IEnumerable<Claim> claims, ClaimsPrincipal user, object resource)
        {
            var context = new AuthorizationPolicyContext(claims, user, resource);

            foreach (var policy in _policies)
            {
                await policy.ApplyingAsync(context);
            }

            // we only apply the policies for a limited number of times to prevent
            // infinite loops

            int retries;
            for (retries = 0; retries < MaxRetries; retries++)
            {
                // we don't need to check for owned claims if the permission is already granted
                if (!context.Authorized)
                {
                    if (context.User != null)
                    {
                        if (ClaimsMatch(context.Claims, context.UserClaims))
                        {
                            context.Authorized = true;
                        }
                    }
                }

                // reset the retry flag
                context.Retry = false;

                // give a chance for policies to change claims or the grant
                foreach (var policy in _policies)
                {
                    await policy.ApplyAsync(context);
                }

                // if no policies have changed the context, stop checking
                if (!context.Retry)
                {
                    break;
                }
            }

            if (retries == MaxRetries)
            {
                throw new InvalidOperationException("Too many authorization retries.");
            }

            foreach (var policy in _policies)
            {
                await policy.AppliedAsync(context);
            }

            return context.Authorized;
        }

        public bool Authorize(IEnumerable<Claim> claims, ClaimsPrincipal user, object resource)
        {
            return AuthorizeAsync(claims, user, resource).Result;
        }

        private bool ClaimsMatch([NotNull] IEnumerable<Claim> x, [NotNull] IEnumerable<Claim> y)
        {
            return x.Any(claim => 
                        y.Any(userClaim => 
                            string.Equals(claim.Type, userClaim.Type, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(claim.Value, userClaim.Value, StringComparison.Ordinal)
                        )
                    );

        }
    }
}