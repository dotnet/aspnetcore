// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Identity;

namespace System.Security.Principal
{
    /// <summary>
    /// Claims related extensions for <see cref="IPrincipal"/>.
    /// </summary>
    public static class PrincipalExtensions
    {
        /// <summary>
        /// Returns the Name claim value if present otherwise returns null.
        /// </summary>
        /// <param name="principal">The <see cref="IPrincipal"/> instance this method extends.</param>
        /// <returns>The Name claim value, or null if the claim is not present.</returns>
        /// <remarks>The name claim is identified by <see cref="ClaimsIdentity.DefaultNameClaimType"/>.</remarks>
        public static string GetUserName(this IPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            var cp = principal as ClaimsPrincipal;
            return cp != null ? cp.FindFirstValue(ClaimsIdentity.DefaultNameClaimType) : null;
        }

        /// <summary>
        /// Returns the User ID claim value if present otherwise returns null.
        /// </summary>
        /// <param name="principal">The <see cref="IPrincipal"/> instance this method extends.</param>
        /// <returns>The User ID claim value, or null if the claim is not present.</returns>
        /// <remarks>The name claim is identified by <see cref="ClaimTypes.NameIdentifier"/>.</remarks>
        public static string GetUserId(this IPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            var ci = principal as ClaimsPrincipal;
            return ci != null ? ci.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        }

        /// <summary>
        /// Returns true if the principal has an identity with the application cookie identity
        /// </summary>
        /// <param name="principal">The <see cref="IPrincipal"/> instance this method extends.</param>
        /// <returns>True if the user is logged in with identity.</returns>
        public static bool IsSignedIn(this IPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            var p = principal as ClaimsPrincipal;
            return p?.Identities != null && 
                p.Identities.Any(i => i.AuthenticationType == IdentityOptions.ApplicationCookieAuthenticationType);
        }

        /// <summary>
        /// Returns the value for the first claim of the specified type otherwise null the claim is not present.
        /// </summary>
        /// <param name="identity">The <see cref="IIdentity"/> instance this method extends.</param>
        /// <param name="claimType">The claim type whose first value should be returned.</param>
        /// <returns>The value of the first instance of the specifed claim type, or null if the claim is not present.</returns>
        public static string FindFirstValue(this ClaimsPrincipal principal, string claimType)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            var claim = principal.FindFirst(claimType);
            return claim != null ? claim.Value : null;
        }

    }
}