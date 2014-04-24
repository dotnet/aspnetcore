// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security.Authorization
{
    public class DefaultAuthorizationService : IAuthorizationService
    {
        private readonly IList<IAuthorizationPolicy> _policies;
        public int MaxRetries = 99;

        public DefaultAuthorizationService(IEnumerable<IAuthorizationPolicy> policies)
        {
            _policies = policies.OrderBy(x => x.Order).ToArray();
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
                        if (context.Claims.Any(claim => user.HasClaim(claim.Type, claim.Value)))
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
    }
}