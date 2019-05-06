// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization.Policy
{
    public interface ICommonPolicyEvaluator
    {
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
        Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, bool authenticationSucceeded, ClaimsPrincipal user, object resource);
    }
}
