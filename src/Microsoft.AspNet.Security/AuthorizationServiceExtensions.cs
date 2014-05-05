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
    public static class AuthorizationServiceExtensions
    {
        /// <summary>
        /// Checks if a user has specific claims.
        /// </summary>
        /// <param name="claim">The claim to check against a specific user.</param>
        /// <param name="user">The user to check claims against.</param>
        /// <returns><value>true</value> when the user fulfills one of the claims, <value>false</value> otherwise.</returns>
        public static Task<bool> AuthorizeAsync(this IAuthorizationService service, Claim claim, ClaimsPrincipal user)
        {
            return service.AuthorizeAsync(new Claim[] { claim }, user);
        }

        /// <summary>
        /// Checks if a user has specific claims.
        /// </summary>
        /// <param name="claim">The claim to check against a specific user.</param>
        /// <param name="user">The user to check claims against.</param>
        /// <returns><value>true</value> when the user fulfills one of the claims, <value>false</value> otherwise.</returns>
        public static bool Authorize(this IAuthorizationService service, Claim claim, ClaimsPrincipal user)
        {
            return service.Authorize(new Claim[] { claim }, user);
        }

        /// <summary>
        /// Checks if a user has specific claims for a specific context obj.
        /// </summary>
        /// <param name="claim">The claim to check against a specific user.</param>
        /// <param name="user">The user to check claims against.</param>
        /// <param name="resource">The resource the claims should be check with.</param>
        /// <returns><value>true</value> when the user fulfills one of the claims, <value>false</value> otherwise.</returns>
        public static Task<bool> AuthorizeAsync(this IAuthorizationService service, Claim claim, ClaimsPrincipal user, object resource)
        {
            return service.AuthorizeAsync(new Claim[] { claim }, user, resource);
        }

        /// <summary>
        /// Checks if a user has specific claims for a specific context obj.
        /// </summary>
        /// <param name="claim">The claimsto check against a specific user.</param>
        /// <param name="user">The user to check claims against.</param>
        /// <param name="resource">The resource the claims should be check with.</param>
        /// <returns><value>true</value> when the user fulfills one of the claims, <value>false</value> otherwise.</returns>
        public static bool Authorize(this IAuthorizationService service, Claim claim, ClaimsPrincipal user, object resource)
        {
            return service.Authorize(new Claim[] { claim }, user, resource);
        }
    }
}