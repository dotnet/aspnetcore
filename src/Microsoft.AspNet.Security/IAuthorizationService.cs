// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Security
{
    /// <summary>
    /// Checks policy based permissions for a user
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Checks if a user meets a specific authorization policy
        /// </summary>
        /// <param name="policy">The policy to check against a specific context.</param>
        /// <param name="context">The HttpContext to check the policy against.</param>
        /// <param name="resource">The resource the policy should be checked with.</param>
        /// <returns><value>true</value> when the user fulfills the policy, <value>false</value> otherwise.</returns>
        Task<bool> AuthorizeAsync(string policyName, HttpContext context, object resource = null);

        /// <summary>
        /// Checks if a user meets a specific authorization policy
        /// </summary>
        /// <param name="policy">The policy to check against a specific context.</param>
        /// <param name="context">The HttpContext to check the policy against.</param>
        /// <param name="resource">The resource the policy should be checked with.</param>
        /// <returns><value>true</value> when the user fulfills the policy, <value>false</value> otherwise.</returns>
        Task<bool> AuthorizeAsync(AuthorizationPolicy policy, HttpContext context, object resource = null);
    }
}