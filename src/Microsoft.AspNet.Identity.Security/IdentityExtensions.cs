// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace System.Security.Principal
{
    /// <summary>
    ///     Extensions making it easier to get the user name/user id claims off of an identity
    /// </summary>
    public static class IdentityExtensions
    {
        /// <summary>
        ///     Return the user name using the UserNameClaimType
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public static string GetUserName(this IIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            var ci = identity as ClaimsIdentity;
            return ci != null ? ci.FindFirstValue(ClaimsIdentity.DefaultNameClaimType) : null;
        }

        /// <summary>
        ///     Return the user id using the UserIdClaimType
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public static string GetUserId(this IIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            var ci = identity as ClaimsIdentity;
            return ci != null ? ci.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        }

        /// <summary>
        ///     Return the claim value for the first claim with the specified type if it exists, null otherwise
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="claimType"></param>
        /// <returns></returns>
        public static string FindFirstValue(this ClaimsIdentity identity, string claimType)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            var claim = identity.FindFirst(claimType);
            return claim != null ? claim.Value : null;
        }
    }
}