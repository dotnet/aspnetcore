// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authorization
{
    public static class AuthorizationServiceExtensions
    {
        /// <summary>
        /// Checks if a user meets a specific authorization policy
        /// </summary>
        /// <param name="service">The authorization service.</param>
        /// <param name="user">The user to check the policy against.</param>
        /// <param name="resource">The resource the policy should be checked with.</param>
        /// <param name="policy">The policy to check against a specific context.</param>
        /// <returns><value>true</value> when the user fulfills the policy, <value>false</value> otherwise.</returns>
        public static Task<bool> AuthorizeAsync([NotNull] this IAuthorizationService service, ClaimsPrincipal user, object resource, [NotNull] AuthorizationPolicy policy)
        {
            return service.AuthorizeAsync(user, resource, policy.Requirements.ToArray());
        }

        /// <summary>
        /// Checks if a user meets a specific authorization policy
        /// </summary>
        /// <param name="service">The authorization service.</param>
        /// <param name="user">The user to check the policy against.</param>
        /// <param name="resource">The resource the policy should be checked with.</param>
        /// <param name="policy">The policy to check against a specific context.</param>
        /// <returns><value>true</value> when the user fulfills the policy, <value>false</value> otherwise.</returns>
        public static bool Authorize([NotNull] this IAuthorizationService service, ClaimsPrincipal user, object resource, [NotNull] AuthorizationPolicy policy)
        {
            return service.Authorize(user, resource, policy.Requirements.ToArray());
        }

    }
}