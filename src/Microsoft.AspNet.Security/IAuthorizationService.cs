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

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security
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