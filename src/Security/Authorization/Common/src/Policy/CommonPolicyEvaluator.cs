// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization.Policy
{
    public class CommonPolicyEvaluator : ICommonPolicyEvaluator
    {
        private readonly IAuthorizationService _authorization;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="authorization">The authorization service.</param>
        public CommonPolicyEvaluator(IAuthorizationService authorization)
        {
            _authorization = authorization;
        }

        /// <summary>
        /// Attempts authorization for a policy using <see cref="IAuthorizationService"/>.
        /// </summary>
        /// <param name="policy">The <see cref="AuthorizationPolicy"/>.</param>
        /// <param name="authenticationSucceeded">True if authentication succeeded, otherwise false.</param>
        /// <param name="user">The <see cref="ClaimsPrincipal"/>.</param>
        /// <param name="resource">
        /// An optional resource the policy should be checked with.
        /// If a resource is not required for policy evaluation you may pass null as the value.
        /// </param>
        /// <returns>Returns <see cref="PolicyAuthorizationResult.Success"/> if authorization succeeds.
        /// Otherwise returns <see cref="PolicyAuthorizationResult.Forbid"/> if <see cref="AuthenticateResult.Succeeded"/>, otherwise
        /// returns  <see cref="PolicyAuthorizationResult.Challenge"/></returns>
        public virtual async Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, bool authenticationSucceeded, ClaimsPrincipal user, object resource)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var result = await _authorization.AuthorizeAsync(user, resource, policy);
            if (result.Succeeded)
            {
                return PolicyAuthorizationResult.Success();
            }

            // If authentication was successful, return forbidden, otherwise challenge
            return authenticationSucceeded
                ? PolicyAuthorizationResult.Forbid()
                : PolicyAuthorizationResult.Challenge();
        }
    }
}
