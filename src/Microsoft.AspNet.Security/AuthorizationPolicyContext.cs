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
using System.Linq;

namespace Microsoft.AspNet.Security
{
    /// <summary>
    /// Contains authorization information used by <see cref="IAuthorizationPolicy"/>.
    /// </summary>
    public class AuthorizationPolicyContext
    {
        public AuthorizationPolicyContext(IEnumerable<Claim> claims, ClaimsPrincipal user, object resource )
        {
            Claims = (claims ?? Enumerable.Empty<Claim>()).ToList();
            User = user;
            Resource = resource;

            // user claims are copied to a new and mutable list
            UserClaims = user != null
                ? user.Claims.ToList()
                : new List<Claim>();
        }

        /// <summary>
        /// The list of claims the <see cref="IAuthorizationService"/> is checking.
        /// </summary>
        public IList<Claim> Claims { get; private set; }

        /// <summary>
        /// The user to check the claims against.
        /// </summary>
        public ClaimsPrincipal User { get; private set; }

        /// <summary>
        /// The claims of the user.
        /// </summary>
        /// <remarks>
        /// This list can be modified by policies for retries.
        /// </remarks>
        public IList<Claim> UserClaims { get; private set; }

        /// <summary>
        /// An optional resource associated to the check.
        /// </summary>
        public object Resource { get; private set; }

        /// <summary>
        /// Gets or set whether the permission will be granted to the user.
        /// </summary>
        public bool Authorized { get; set; }

        /// <summary>
        /// When set to <value>true</value>, the authorization check will be processed again.
        /// </summary>
        public bool Retry { get; set; }
    }
}
