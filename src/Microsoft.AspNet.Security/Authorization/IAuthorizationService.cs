// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security.Authorization
{
    /// <summary>
    /// Checks claims based permissions for a user.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Checks if a user has specific claims.
        /// </summary>
        /// <param name="claims">The claims to check against a specific user.</param>
        /// <param name="user">The user to check claims against.</param>
        /// <returns><value>true</value> when the user fulfills one of the claims, <value>false</value> otherwise.</returns>
        Task<bool> AuthorizeAsync(IEnumerable<Claim> claims, ClaimsPrincipal user);

        /// <summary>
        /// Checks if a user has specific claims.
        /// </summary>
        /// <param name="claims">The claims to check against a specific user.</param>
        /// <param name="user">The user to check claims against.</param>
        /// <returns><value>true</value> when the user fulfills one of the claims, <value>false</value> otherwise.</returns>
        bool Authorize(IEnumerable<Claim> claims, ClaimsPrincipal user);

        /// <summary>
        /// Checks if a user has specific claims for a specific context obj.
        /// </summary>
        /// <param name="claims">The claims to check against a specific user.</param>
        /// <param name="user">The user to check claims against.</param>
        /// <param name="resource">The resource the claims should be check with.</param>
        /// <returns><value>true</value> when the user fulfills one of the claims, <value>false</value> otherwise.</returns>
        Task<bool> AuthorizeAsync(IEnumerable<Claim> claims, ClaimsPrincipal user, object resource);

        /// <summary>
        /// Checks if a user has specific claims for a specific context obj.
        /// </summary>
        /// <param name="claims">The claims to check against a specific user.</param>
        /// <param name="user">The user to check claims against.</param>
        /// <param name="resource">The resource the claims should be check with.</param>
        /// <returns><value>true</value> when the user fulfills one of the claims, <value>false</value> otherwise.</returns>
        bool Authorize(IEnumerable<Claim> claims, ClaimsPrincipal user, object resource);

    }
}